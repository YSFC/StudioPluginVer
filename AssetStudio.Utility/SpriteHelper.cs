using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using NumSharp;
using SixLabors.ImageSharp.Advanced;

namespace AssetStudio
{
	public static class SpriteHelper
	{
		private static Configuration _configuration;

		static SpriteHelper()
		{
			_configuration = Configuration.Default.Clone();
			_configuration.PreferContiguousImageBuffers = true;
		}
		public static Image<Bgra32> GetImage(this Sprite m_Sprite)
		{
			if (m_Sprite.m_SpriteAtlas != null && m_Sprite.m_SpriteAtlas.TryGet(out var m_SpriteAtlas))
			{
				if (m_SpriteAtlas.m_RenderDataMap.TryGetValue(m_Sprite.m_RenderDataKey, out var spriteAtlasData) && spriteAtlasData.texture.TryGet(out var m_Texture2D))
				{
					return CutImage(m_Sprite, m_Texture2D, spriteAtlasData.textureRect, spriteAtlasData.textureRectOffset, spriteAtlasData.downscaleMultiplier, spriteAtlasData.settingsRaw);
				}
			}
			else
			{
				if (m_Sprite.reader.Game.Type.IsPathToNowhere() && m_Sprite.m_RD.texture.TryGet(out var m_Texture2D))
				{
					return GetPtNSprite(m_Sprite, m_Texture2D, m_Sprite.m_RD.downscaleMultiplier, m_Sprite.m_RD.settingsRaw) ?? CutImage(m_Sprite, m_Texture2D, m_Sprite.m_RD.textureRect, m_Sprite.m_RD.textureRectOffset, m_Sprite.m_RD.downscaleMultiplier, m_Sprite.m_RD.settingsRaw);
				}
				if (m_Sprite.m_RD.texture.TryGet(out m_Texture2D))
				{
					return CutImage(m_Sprite, m_Texture2D, m_Sprite.m_RD.textureRect, m_Sprite.m_RD.textureRectOffset, m_Sprite.m_RD.downscaleMultiplier, m_Sprite.m_RD.settingsRaw);
				}
			}
			return null;
		}

		private static Image<Bgra32> CutImage(Sprite m_Sprite, Texture2D m_Texture2D, Rectf textureRect, Vector2 textureRectOffset, float downscaleMultiplier, SpriteSettings settingsRaw)
		{
			var originalImage = m_Texture2D.ConvertToImage(false);
			if (originalImage != null)
			{
				using (originalImage)
				{
					if (downscaleMultiplier > 0f && downscaleMultiplier != 1f)
					{
						var width = (int)(m_Texture2D.m_Width / downscaleMultiplier);
						var height = (int)(m_Texture2D.m_Height / downscaleMultiplier);
						originalImage.Mutate(x => x.Resize(width, height));
					}
					var rectX = (int)Math.Floor(textureRect.x);
					var rectY = (int)Math.Floor(textureRect.y);
					var rectRight = (int)Math.Ceiling(textureRect.x + textureRect.width);
					var rectBottom = (int)Math.Ceiling(textureRect.y + textureRect.height);
					rectRight = Math.Min(rectRight, originalImage.Width);
					rectBottom = Math.Min(rectBottom, originalImage.Height);
					var rect = new Rectangle(rectX, rectY, rectRight - rectX, rectBottom - rectY);
					var spriteImage = originalImage.Clone(x => x.Crop(rect));
					if (settingsRaw.packed == 1)
					{
						//RotateAndFlip
						switch (settingsRaw.packingRotation)
						{
							case SpritePackingRotation.FlipHorizontal:
								spriteImage.Mutate(x => x.Flip(FlipMode.Horizontal));
								break;
							case SpritePackingRotation.FlipVertical:
								spriteImage.Mutate(x => x.Flip(FlipMode.Vertical));
								break;
							case SpritePackingRotation.Rotate180:
								spriteImage.Mutate(x => x.Rotate(180));
								break;
							case SpritePackingRotation.Rotate90:
								spriteImage.Mutate(x => x.Rotate(270));
								break;
						}
					}

					//Tight
					if (settingsRaw.packingMode == SpritePackingMode.Tight)
					{
						try
						{
							var triangles = GetTriangles(m_Sprite.m_RD);
							var polygons = triangles.Select(x => new Polygon(new LinearLineSegment(x.Select(y => new PointF(y.X, y.Y)).ToArray()))).ToArray();
							IPathCollection path = new PathCollection(polygons);
							var matrix = Matrix3x2.CreateScale(m_Sprite.m_PixelsToUnits);
							matrix *= Matrix3x2.CreateTranslation(m_Sprite.m_Rect.width * m_Sprite.m_Pivot.X - textureRectOffset.X, m_Sprite.m_Rect.height * m_Sprite.m_Pivot.Y - textureRectOffset.Y);
							path = path.Transform(matrix);
							var options = new DrawingOptions
							{
								GraphicsOptions = new GraphicsOptions
								{
									Antialias = false,
									AlphaCompositionMode = PixelAlphaCompositionMode.DestOut
								}
							};
							using (var mask = new Image<Bgra32>(rect.Width, rect.Height, SixLabors.ImageSharp.Color.Black))
							{
								mask.Mutate(x => x.Fill(options, SixLabors.ImageSharp.Color.Red, path));
								var bursh = new ImageBrush(mask);
								spriteImage.Mutate(x => x.Fill(options, bursh));
								spriteImage.Mutate(x => x.Flip(FlipMode.Vertical));
								return spriteImage;
							}
						}
						catch
						{
							// ignored
						}
					}

					//Rectangle
					spriteImage.Mutate(x => x.Flip(FlipMode.Vertical));
					return spriteImage;
				}
			}

			return null;
		}

		private static Vector2[][] GetTriangles(SpriteRenderData m_RD)
		{
			if (m_RD.vertices != null) //5.6 down
			{
				var vertices = m_RD.vertices.Select(x => (Vector2)x.pos).ToArray();
				var triangleCount = m_RD.indices.Length / 3;
				var triangles = new Vector2[triangleCount][];
				for (int i = 0; i < triangleCount; i++)
				{
					var first = m_RD.indices[i * 3];
					var second = m_RD.indices[i * 3 + 1];
					var third = m_RD.indices[i * 3 + 2];
					var triangle = new[] { vertices[first], vertices[second], vertices[third] };
					triangles[i] = triangle;
				}
				return triangles;
			}
			else //5.6 and up
			{
				var triangles = new List<Vector2[]>();
				var m_VertexData = m_RD.m_VertexData;
				var m_Channel = m_VertexData.m_Channels[0]; //kShaderChannelVertex
				var m_Stream = m_VertexData.m_Streams[m_Channel.stream];
				using (var vertexReader = new EndianBinaryReader(new MemoryStream(m_VertexData.m_DataSize), EndianType.LittleEndian))
				{
					using (var indexReader = new EndianBinaryReader(new MemoryStream(m_RD.m_IndexBuffer), EndianType.LittleEndian))
					{
						foreach (var subMesh in m_RD.m_SubMeshes)
						{
							vertexReader.BaseStream.Position = m_Stream.offset + subMesh.firstVertex * m_Stream.stride + m_Channel.offset;

							var vertices = new Vector2[subMesh.vertexCount];
							for (int v = 0; v < subMesh.vertexCount; v++)
							{
								vertices[v] = new Vector3(vertexReader.ReadSingle(), vertexReader.ReadSingle(), vertexReader.ReadSingle());
								vertexReader.BaseStream.Position += m_Stream.stride - 12;
							}

							indexReader.BaseStream.Position = subMesh.firstByte;

							var triangleCount = subMesh.indexCount / 3u;
							for (int i = 0; i < triangleCount; i++)
							{
								var first = indexReader.ReadUInt16() - subMesh.firstVertex;
								var second = indexReader.ReadUInt16() - subMesh.firstVertex;
								var third = indexReader.ReadUInt16() - subMesh.firstVertex;
								var triangle = new[] { vertices[first], vertices[second], vertices[third] };
								triangles.Add(triangle);
							}
						}
					}
				}
				return triangles.ToArray();
			}
		}

		private static Image<Bgra32> GetPtNSprite(Sprite m_Sprite, Texture2D m_Texture2D, float downscaleMultiplier, SpriteSettings settingsRaw)
		{
			Image<Bgra32> originalImage = m_Texture2D.ConvertToImage(false);
			if (originalImage != null)
			{
				if (downscaleMultiplier > 0f && downscaleMultiplier != 1f)
				{
					int width = (int)((float)m_Texture2D.m_Width / downscaleMultiplier);
					int height = (int)((float)m_Texture2D.m_Height / downscaleMultiplier);
					originalImage.Mutate(delegate (IImageProcessingContext x)
					{
						x.Resize(width, height);
					});
				}
				Size texSize = originalImage.Size();
				if (settingsRaw.packingMode == SpritePackingMode.Tight)
				{
					try
					{
						Image<Bgra32> image = BuildPtNSprite(m_Sprite.m_RD, m_Sprite.m_PixelsToUnits, texSize, originalImage);
						originalImage.Dispose();
						if (image != null)
						{
							image.Mutate(delegate (IImageProcessingContext x)
							{
								x.Flip(FlipMode.Vertical);
							});
						}
						return image;
					}
					catch (Exception e)
					{
						Logger.Error("Error.", e);
					}
				}
			}
			return null;
		}


		private static NDArray LoadChannelData(VertexData m_VertexData, SubMesh subMesh, int channelId)
		{
			ChannelInfo m_Channel = m_VertexData.m_Channels[channelId];
			StreamInfo m_Stream = m_VertexData.m_Streams[(int)m_Channel.stream];
			uint offset = m_Stream.offset + subMesh.firstVertex * m_Stream.stride + (uint)m_Channel.offset;
			uint length = subMesh.vertexCount * (uint)m_Channel.dimension;
			NDArray result;
			using (EndianBinaryReader vertexReader = new EndianBinaryReader(new MemoryStream(m_VertexData.m_DataSize), EndianType.LittleEndian))
			{
				vertexReader.BaseStream.Position = (long)((ulong)offset);
				float[] vertexArray = vertexReader.ReadSingleArray((int)length);
				result = np.ndarray(new int[]
				{
					vertexArray.Length / (int)m_Channel.dimension,
					(int)m_Channel.dimension
				}, np.float32, vertexArray, 'C');
			}
			return result;
		}

		private static Image<Bgra32> BuildPtNSprite(SpriteRenderData m_RD, float m_PixelsToUnits, Size texSize, Image<Bgra32> sourceTex)
		{
			new List<Vector2[]>();
			VertexData m_VertexData = m_RD.m_VertexData;
			var subMeshes = m_RD.m_SubMeshes;
			int num = 0;
			if (num >= subMeshes.Count())
			{
				return null;
			}
			SubMesh subMesh = subMeshes[num];
			NDArray positions = LoadChannelData(m_VertexData, subMesh, 0);
			NDArray uv = LoadChannelData(m_VertexData, subMesh, 4);
			if (uv.sum() == 0)
			{
				return null;
			}
			NDArray npTriangles;
			using (EndianBinaryReader indexReader = new EndianBinaryReader(new MemoryStream(m_RD.m_IndexBuffer), EndianType.LittleEndian))
			{
				indexReader.BaseStream.Position = (long)((ulong)subMesh.firstByte);
				int[] indexArray = indexReader.ReadUInt16ArrayToInt32Array((int)subMesh.indexCount);
				npTriangles = np.ndarray(new int[]
				{
					indexArray.Length / 3,
					3
				}, np.int32, indexArray, 'C');
			}
			List<int> zeroAxisList = new List<int>();
			NDArray ndarray2;
			for (int k = 0; k < 3; k++)
			{
				NDArray ndarray = positions;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(3, 1);
				defaultInterpolatedStringHandler.AppendLiteral(":, ");
				defaultInterpolatedStringHandler.AppendFormatted<int>(k);
				ndarray2 = ndarray[defaultInterpolatedStringHandler.ToStringAndClear()];
				if (np.unique(ndarray2).size == 1)
				{
					zeroAxisList.Add(k);
				}
			}
			if (zeroAxisList.Count != 1)
			{
				Logger.Warning("Can't process 3d sprites!");
				return null;
			}
			int zeroAxis = zeroAxisList[0];
			var pos2dArray = positions.flatten('C').ToArray<float>().Where((float x, int i) => (i + 1) % (zeroAxis + 1) != 0).ToArray();
			positions = np.ndarray(new int[]
			{
				pos2dArray.Length / 2,
				2
			}, np.float32, pos2dArray, 'C');
			positions -= positions.min(0, false, null);
			ndarray2 = positions * m_PixelsToUnits;
			NDArray positions_abs = np.round_(ndarray2).astype(np.int32, true);
			ndarray2 = uv * new int[]
			{
				texSize.Width,
				texSize.Height
			};
			NDArray uv_abs = np.round_(ndarray2).astype(np.int32, true);
			//var size = positions_abs.max(0, false, null).ToArray<int>();
			var size = new int[] { (int)Math.Round(m_RD.textureRect.width), (int)Math.Round(m_RD.textureRect.height) };
			Size spriteSize = new Size(size[0], size[1]);
			Image<Bgra32> spriteImg = new Image<Bgra32>(_configuration, spriteSize.Width, spriteSize.Height, SixLabors.ImageSharp.Color.Transparent);
			for (int j = 0; j < npTriangles.shape[0]; j++)
			{
				NDArray tri = npTriangles[new Slice[] { j }];
				NDArray srcTri = uv_abs[new object[] { tri }];
				NDArray dstTri = positions_abs[new object[] { tri }];
				var upperLeft = srcTri.min(0, false, null).ToArray<int>();
				var lowerRight = srcTri.max(0, false, null).ToArray<int>();
				Rectangle rect = new Rectangle(upperLeft[0], upperLeft[1], lowerRight[0] - upperLeft[0], lowerRight[1] - upperLeft[1]);
				Image<Bgra32> srcPart = sourceTex.Clone(delegate (IImageProcessingContext x) { x.Crop(rect); });
				var dstPos = dstTri.min(0, false, null).ToArray<int>();
				spriteImg.Mutate(delegate (IImageProcessingContext x) { x.DrawImage(srcPart, new Point(dstPos[0], dstPos[1]), 1f); });
			}
			return spriteImg;
		}
	}
}
