using AssetStudio;
using AssetStudio.GUI;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using ZstdSharp.Unsafe;

namespace UnityLive2DExtractor
{
    public class Util
    {
        [Flags]
        public enum MPType
        {
            GetMocByClassName = 1,
            GetMocByHeader = 2,
            GetPhysicByClassName = 4,
            GetPhysicBySubRig = 8,
        }

        public static void CreateMotion3Json(GameObject gameObject, List<AnimationClip> animationClips, Action<GameObject, string, string> JsonMap)
        {
            var rootTransform = gameObject.m_Transform;
            while (rootTransform.m_Father.TryGet(out var m_Father))
            {
                rootTransform = m_Father;
            }
            rootTransform.m_GameObject.TryGet(out var rootGameObject);
            var converter = new CubismMotion3Converter(rootGameObject, animationClips.ToArray());
            foreach (ImportedKeyframedAnimation animation in converter.AnimationList)
            {
                var json = new CubismMotion3Json
                {
                    Version = 3,
                    Meta = new CubismMotion3Json.SerializableMeta
                    {
                        Duration = animation.Duration,
                        Fps = animation.SampleRate,
                        Loop = true,
                        AreBeziersRestricted = true,
                        CurveCount = animation.TrackList.Count,
                        UserDataCount = animation.Events.Count
                    },
                    Curves = new CubismMotion3Json.SerializableCurve[animation.TrackList.Count]
                };
                int totalSegmentCount = 1;
                int totalPointCount = 1;
                for (int i = 0; i < animation.TrackList.Count; i++)
                {
                    var track = animation.TrackList[i];
                    json.Curves[i] = new CubismMotion3Json.SerializableCurve
                    {
                        Target = track.Target,
                        Id = track.Name,
                        Segments = new List<float> { 0f, track.Curve[0].value }
                    };
                    for (var j = 1; j < track.Curve.Count; j++)
                    {
                        var curve = track.Curve[j];
                        var preCurve = track.Curve[j - 1];
                        if (Math.Abs(curve.time - preCurve.time - 0.01f) < 0.0001f) //InverseSteppedSegment
                        {
                            var nextCurve = track.Curve[j + 1];
                            if (nextCurve.value == curve.value)
                            {
                                json.Curves[i].Segments.Add(3f);
                                json.Curves[i].Segments.Add(nextCurve.time);
                                json.Curves[i].Segments.Add(nextCurve.value);
                                j += 1;
                                totalPointCount += 1;
                                totalSegmentCount++;
                                continue;
                            }
                        }
                        if (float.IsPositiveInfinity(curve.inSlope)) //SteppedSegment
                        {
                            json.Curves[i].Segments.Add(2f);
                            json.Curves[i].Segments.Add(curve.time);
                            json.Curves[i].Segments.Add(curve.value);
                            totalPointCount += 1;
                        }
                        else if (preCurve.outSlope == 0f && Math.Abs(curve.inSlope) < 0.0001f) //LinearSegment
                        {
                            json.Curves[i].Segments.Add(0f);
                            json.Curves[i].Segments.Add(curve.time);
                            json.Curves[i].Segments.Add(curve.value);
                            totalPointCount += 1;
                        }
                        else //BezierSegment
                        {
                            var tangentLength = (curve.time - preCurve.time) / 3f;
                            json.Curves[i].Segments.Add(1f);
                            json.Curves[i].Segments.Add(preCurve.time + tangentLength);
                            json.Curves[i].Segments.Add(preCurve.outSlope * tangentLength + preCurve.value);
                            json.Curves[i].Segments.Add(curve.time - tangentLength);
                            json.Curves[i].Segments.Add(curve.value - curve.inSlope * tangentLength);
                            json.Curves[i].Segments.Add(curve.time);
                            json.Curves[i].Segments.Add(curve.value);
                            totalPointCount += 3;
                        }
                        totalSegmentCount++;
                    }
                }
                json.Meta.TotalSegmentCount = totalSegmentCount;
                json.Meta.TotalPointCount = totalPointCount;

                json.UserData = new CubismMotion3Json.SerializableUserData[animation.Events.Count];
                var totalUserDataSize = 0;
                for (var i = 0; i < animation.Events.Count; i++)
                {
                    var @event = animation.Events[i];
                    json.UserData[i] = new CubismMotion3Json.SerializableUserData
                    {
                        Time = @event.time,
                        Value = @event.value
                    };
                    totalUserDataSize += @event.value.Length;
                }
                json.Meta.TotalUserDataSize = totalUserDataSize;
                JsonMap?.Invoke(gameObject, animation.Name, JsonConvert.SerializeObject(json, Formatting.Indented, new MyJsonConverter()));

                // motions.Add($"motions/{animation.Name}.motion3.json");
                //File.WriteAllText($"{destAnimationPath}{animation.Name}.motion3.json", JsonConvert.SerializeObject(json, Formatting.Indented, new MyJsonConverter()));
            }
        }

        public static void CreateMotion3Json(Animator animator, List<AnimationClip> animationClips, Action<GameObject, string, string> JsonMap)
        {
            if (animator.m_GameObject.TryGet(out GameObject gameObject))
            {
                CreateMotion3Json(gameObject, animationClips, JsonMap);
            }
        }

        public static void CreateMotion3Json(MonoBehaviour monoBehaviour, List<AnimationClip> animationClips, Action<GameObject, string, string> JsonMap)
        {
            if (monoBehaviour.m_GameObject.TryGet(out GameObject gameObject))
            {
                CreateMotion3Json(gameObject, animationClips, JsonMap);
            }
        }

        public static void GetL2DByAnimator(string folder,bool nameWithIndex)
        {
            AssetList.GetSelectedAssets().ApplyFunc<AssetItem, Animator>(x =>
            {
                if (x.Type == ClassIDType.Animator)
                {
                    var animator = x.Asset as Animator;
                    if (animator.m_GameObject.TryGet(out var gameObject))
                    {
                        List<string> message = new List<string>
                        {
                            $"{gameObject.m_PathID}:{gameObject.m_Name}"
                        };
                        bool flag = false;
                        bool withPhyic = false;
                        bool withAnimation = false;
                        byte[] mocByte = null;
                        string physicJson = string.Empty;
                        List<AnimationClip> animationClips = new List<AnimationClip>();
                        foreach (var component in gameObject.m_Components)
                        {
                            if (component.TryGet(out MonoBehaviour monoBehaviour))
                            {
                                var dict = monoBehaviour.ToDictionary();
                                if (!flag && dict.Contains("_moc"))
                                {
                                    var od = dict["_moc"] as OrderedDictionary;
                                    var pptr = new PPtr<MonoBehaviour>((int)od["m_FileID"], (long)od["m_PathID"], monoBehaviour.assetsFile);
                                    if (pptr.TryGet(out MonoBehaviour moc))
                                    {
                                        message.Add($"PathID:{moc.m_PathID}:{moc.m_Name} is target moc file");
                                        flag = true;
                                        mocByte = ParseMoc(moc);
                                        continue;
                                    }
                                }
                                else if (!withPhyic && dict.Contains("_rig"))
                                {
                                    message.Add($"{monoBehaviour.m_PathID}:{monoBehaviour.m_Name} is physic file");
                                    withPhyic = true;
                                    physicJson = ParsePhysics(monoBehaviour);
                                    continue;
                                }
                                if (flag && withPhyic)
                                    break;
                            }

                            //switch (component.Name)
                            //{
                            //    case "CubismModel":
                            //        if (component.TryGet(out MonoBehaviour mocTarget) && mocTarget.TryGetComponent("_moc", out MonoBehaviour moc)/*mocTarget.TryGetToken("_moc", out JToken token)*/)
                            //        {
                            //            //Console.WriteLine($"PathID:{token["m_PathID"]} is target moc file");
                            //            Console.WriteLine($"PathID:{moc.m_PathID}:{mocTarget.m_Name} is target moc file");
                            //            flag = true;
                            //            mocByte = ParseMoc(moc);
                            //        }
                            //        break;

                            //    case "CubismPhysicsController":
                            //        if (component.TryGet(out MonoBehaviour physic))
                            //        {
                            //            Console.WriteLine($"{physic.m_PathID}:{physic.m_Name} is physic file");
                            //            withPhyic = true;
                            //            physicJson = ParsePhysics(physic);
                            //        }
                            //        break;
                            //    default:
                            //        Console.WriteLine($"skip {x.Name}");
                            //        return null;
                            //}
                        }
                        if (!flag)
                        {
                            Console.WriteLine("skip:" + message[0]);
                            return null;
                        }

                        if(animator.m_Controller.TryGet(out AnimatorController controller))
                        {
                            animationClips = controller.m_AnimationClips.ApplyFunc(x=>x.TryGet(out AnimationClip animationClip)?animationClip:null,true).ToList();
                            Console.WriteLine($"get {animationClips.Count} motion");
                            withAnimation = true;
                        }
                        message.ForEach(x => Console.WriteLine(x));
                        var drawables = gameObject.m_Transform.m_Children.FirstOrDefault(x => x.TryGet(out Transform result) && result.m_GameObject.Name == "Drawables");
                        //if (drawables == null)
                        //    return null;
                        drawables.TryGet(out Transform result);
                        var textures = result.m_Children.ApplyFunc(x => (x.TryGet(out Transform artMeshTransform) && artMeshTransform.m_GameObject.TryGet(out GameObject artMesh) &&
                            artMesh.m_Components[4].TryGet(out MonoBehaviour cubismRenderer) &&
                            cubismRenderer.TryGetRefInfo("_mainTexture", out RefInfo token)) ?
                            token : null,
                            true
                        ).ToHashSet(new RefInfo()).ApplyFunc(x => x.TryGet(result, out Texture2D tex) ? tex : null, true).ToList();
                        textures.Sort((x, y) => x.m_Name.CompareTo(y.m_Name));

                        Console.WriteLine($"textures PathID list:{JsonConvert.SerializeObject(textures.Select(x => $"{x.m_PathID}:{x.m_Name}"), Formatting.Indented)}");

                        string root = Path.Combine(folder, gameObject.Name, gameObject.m_PathID.ToString());
                        Directory.CreateDirectory(root);
                        File.WriteAllBytes(Path.Combine(root, gameObject.Name + ".moc3"), mocByte);
                        if (withPhyic)
                        {
                            File.WriteAllText(Path.Combine(root, gameObject.Name + ".physics3.json"), physicJson);
                        }
                        string imgRoot = Path.Combine(root, "Textures");
                        Directory.CreateDirectory(imgRoot);
                        foreach (var texture2D in textures)
                        {
                            var texture2dConverter = new Texture2DConverter(texture2D);
                            var buff = BigArrayPool<byte>.Shared.Rent(texture2D.m_Width * texture2D.m_Height * 4);
                            try
                            {
                                if (texture2dConverter.DecodeTexture2D(buff))
                                {
                                    //textures.Add($"textures/{texture2D.m_Name}.png");
                                    var image = Image.LoadPixelData<Bgra32>(buff, texture2D.m_Width, texture2D.m_Height);
                                    using (image)
                                    {
                                        using var file = File.OpenWrite(Path.Combine(imgRoot, texture2D.m_Name + ".png"));
                                        image.Mutate(x => x.Flip(FlipMode.Vertical));
                                        image.WriteToStream(file, ImageFormat.Png);
                                    }
                                }
                            }
                            finally
                            {
                                BigArrayPool<byte>.Shared.Return(buff);
                            }
                        }
                        if (withAnimation)
                        {
                            int i = 0;
                            CreateMotion3Json(gameObject, animationClips, (g, name, js) =>
                            {
                                string motionRoot = Path.Combine(root, "Motions");
                                Directory.CreateDirectory(motionRoot);
                                if (nameWithIndex)
                                {
                                    name =name+ "_"+i.ToString();
                                }
                                else if(name == "")
                                {
                                    name = i.ToString();
                                }

                                File.WriteAllText(Path.Combine(motionRoot, name + ".motion3.json"), js);
                                i += 1;
                            });
                        }
                        Console.WriteLine(message[0]+"saved!");
                    }
                }

                return null;
            }, true).ToList();
        }

        public static byte[] ParseMoc(MonoBehaviour moc)
        {
            var reader = moc.reader;
            reader.Reset();
            reader.Position += 28; //PPtr<GameObject> m_GameObject, m_Enabled, PPtr<MonoScript>
            reader.ReadAlignedString(); //m_Name
            return reader.ReadBytes(reader.ReadInt32());
        }

        public static string ParsePhysics(MonoBehaviour physics)
        {
            try
            {
                var reader = physics.reader;
                reader.Reset();
                reader.Position += 28; //PPtr<GameObject> m_GameObject, m_Enabled, PPtr<MonoScript>
                reader.ReadAlignedString(); //m_Name
                var cubismPhysicsRig = new CubismPhysicsRig(reader);

                var physicsSettings = new CubismPhysics3Json.SerializablePhysicsSettings[cubismPhysicsRig.SubRigs.Length];
                for (int i = 0; i < physicsSettings.Length; i++)
                {
                    var subRigs = cubismPhysicsRig.SubRigs[i];
                    physicsSettings[i] = new CubismPhysics3Json.SerializablePhysicsSettings
                    {
                        Id = $"PhysicsSetting{i + 1}",
                        Input = new CubismPhysics3Json.SerializableInput[subRigs.Input.Length],
                        Output = new CubismPhysics3Json.SerializableOutput[subRigs.Output.Length],
                        Vertices = new CubismPhysics3Json.SerializableVertex[subRigs.Particles.Length],
                        Normalization = new CubismPhysics3Json.SerializableNormalization
                        {
                            Position = new CubismPhysics3Json.SerializableNormalizationValue
                            {
                                Minimum = subRigs.Normalization.Position.Minimum,
                                Default = subRigs.Normalization.Position.Default,
                                Maximum = subRigs.Normalization.Position.Maximum
                            },
                            Angle = new CubismPhysics3Json.SerializableNormalizationValue
                            {
                                Minimum = subRigs.Normalization.Angle.Minimum,
                                Default = subRigs.Normalization.Angle.Default,
                                Maximum = subRigs.Normalization.Angle.Maximum
                            }
                        }
                    };
                    for (int j = 0; j < subRigs.Input.Length; j++)
                    {
                        var input = subRigs.Input[j];
                        physicsSettings[i].Input[j] = new CubismPhysics3Json.SerializableInput
                        {
                            Source = new CubismPhysics3Json.SerializableParameter
                            {
                                Target = "Parameter", //同名GameObject父节点的名称
                                Id = input.SourceId
                            },
                            Weight = input.Weight,
                            Type = Enum.GetName(typeof(CubismPhysicsSourceComponent), input.SourceComponent),
                            Reflect = input.IsInverted
                        };
                    }
                    for (int j = 0; j < subRigs.Output.Length; j++)
                    {
                        var output = subRigs.Output[j];
                        physicsSettings[i].Output[j] = new CubismPhysics3Json.SerializableOutput
                        {
                            Destination = new CubismPhysics3Json.SerializableParameter
                            {
                                Target = "Parameter", //同名GameObject父节点的名称
                                Id = output.DestinationId
                            },
                            VertexIndex = output.ParticleIndex,
                            Scale = output.AngleScale,
                            Weight = output.Weight,
                            Type = Enum.GetName(typeof(CubismPhysicsSourceComponent), output.SourceComponent),
                            Reflect = output.IsInverted
                        };
                    }
                    for (int j = 0; j < subRigs.Particles.Length; j++)
                    {
                        var particles = subRigs.Particles[j];
                        physicsSettings[i].Vertices[j] = new CubismPhysics3Json.SerializableVertex
                        {
                            Position = new CubismPhysics3Json.SerializableVector2
                            {
                                X = particles.InitialPosition.X,
                                Y = particles.InitialPosition.Y
                            },
                            Mobility = particles.Mobility,
                            Delay = particles.Delay,
                            Acceleration = particles.Acceleration,
                            Radius = particles.Radius
                        };
                    }
                }
                var physicsDictionary = new CubismPhysics3Json.SerializablePhysicsDictionary[physicsSettings.Length];
                for (int i = 0; i < physicsSettings.Length; i++)
                {
                    physicsDictionary[i] = new CubismPhysics3Json.SerializablePhysicsDictionary
                    {
                        Id = $"PhysicsSetting{i + 1}",
                        Name = $"Dummy{i + 1}"
                    };
                }
                var physicsJson = new CubismPhysics3Json
                {
                    Version = 3,
                    Meta = new CubismPhysics3Json.SerializableMeta
                    {
                        PhysicsSettingCount = cubismPhysicsRig.SubRigs.Length,
                        TotalInputCount = cubismPhysicsRig.SubRigs.Sum(x => x.Input.Length),
                        TotalOutputCount = cubismPhysicsRig.SubRigs.Sum(x => x.Output.Length),
                        VertexCount = cubismPhysicsRig.SubRigs.Sum(x => x.Particles.Length),
                        EffectiveForces = new CubismPhysics3Json.SerializableEffectiveForces
                        {
                            Gravity = new CubismPhysics3Json.SerializableVector2
                            {
                                X = 0,
                                Y = -1
                            },
                            Wind = new CubismPhysics3Json.SerializableVector2
                            {
                                X = 0,
                                Y = 0
                            }
                        },
                        PhysicsDictionary = physicsDictionary
                    },
                    PhysicsSettings = physicsSettings
                };
                return JsonConvert.SerializeObject(physicsJson, Formatting.Indented, new MyJsonConverter2());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while exporting physics with name: {physics.m_Name}\nReason: {ex}");
            }

            return string.Empty;
        }

        public static void SaveMotions(string folder, bool withId = false)
        {
            List<AnimationClip> animationClips = new List<AnimationClip>();
            List<Animator> animators = new List<Animator>();
            AssetList.GetSelectedAssets().Apply(x =>
            {
                if (x.Type == ClassIDType.AnimationClip)
                {
                    animationClips.Add(x.Asset as AnimationClip);
                }
                else if (x.Type == ClassIDType.Animator)
                {
                    animators.Add(x.Asset as Animator);
                }
            });
            foreach (Animator animator in animators)
            {
                CreateMotion3Json(animator, animationClips,
                    (gameObject, name, json) =>
                    {
                        string savePath = withId ? Path.Combine(folder, gameObject.Name, gameObject.m_PathID.ToString(), "Motions", name + ".motion3.json") :
                            Path.Combine(folder, gameObject.Name, "Motions", name + ".motion3.json");
                        Directory.CreateDirectory(Directory.GetParent(savePath).ToString());
                        File.WriteAllText(savePath, json);
                    }
                );
            }
        }

        public static void SaveMP(MPType mpType, string folder, bool withId = false)
        {
            AssetList.GetSelectedAssets().Where(x => x.Type == ClassIDType.MonoBehaviour)
                .Apply(x =>
                {
                    var monoBehaviour = x.Asset as MonoBehaviour;
                    if (mpType.HasFlag(MPType.GetMocByClassName))
                    {
                        if (TryGetMocByClassName(monoBehaviour, out byte[] moc))
                        {
                            string savePath = Path.Combine(folder, x.Name + ".moc3");
                            Directory.CreateDirectory(Directory.GetParent(savePath).ToString());
                            File.WriteAllBytes(savePath, moc);
                            x.Selected = false;
                            return;
                        }
                    }
                    if (mpType.HasFlag(MPType.GetMocByHeader))
                    {
                        if (TryGetMocByHeader(monoBehaviour, out byte[] moc))
                        {
                            string savePath = Path.Combine(folder, x.Name + ".moc3");
                            Directory.CreateDirectory(Directory.GetParent(savePath).ToString());
                            File.WriteAllBytes(savePath, moc);
                            x.Selected = false;
                            return;
                        }
                    }
                    if (mpType.HasFlag(MPType.GetMocByClassName))
                    {
                        if (TryGetPhysicByClassName(monoBehaviour, out string physic))
                        {
                            monoBehaviour.m_GameObject.TryGet(out GameObject gameObject);
                            string savePath;
                            if (withId)
                            {
                                savePath = Path.Combine(folder, gameObject.Name, gameObject.m_PathID.ToString(), gameObject.Name/*x.Name*/ + ".physics3.json");
                            }
                            else
                            {
                                savePath = Path.Combine(folder, gameObject.Name, gameObject.Name/*x.Name*/ + ".physics3.json");
                            }
                            Directory.CreateDirectory(Directory.GetParent(savePath).ToString());
                            File.WriteAllText(savePath, physic);
                            return;
                        }
                    }
                    if (mpType.HasFlag(MPType.GetPhysicBySubRig))
                    {
                        if (TryGetPhysicBySubRig(monoBehaviour, out string physic))
                        {
                            monoBehaviour.m_GameObject.TryGet(out GameObject gameObject);
                            string savePath;
                            if (withId)
                            {
                                savePath = Path.Combine(folder, gameObject.Name, gameObject.m_PathID.ToString(), gameObject.Name/*x.Name*/ + ".physics3.json");
                            }
                            else
                            {
                                savePath = Path.Combine(folder, gameObject.Name, gameObject.Name/*x.Name*/ + ".physics3.json");
                            }
                            Directory.CreateDirectory(Directory.GetParent(savePath).ToString());
                            File.WriteAllText(savePath, physic);
                            return;
                        }
                    }
                    x.Selected = false;
                });
        }

        public static bool TryGetMocByClassName(MonoBehaviour monoBehaviour, out byte[] moc)
        {
            if (monoBehaviour.m_Script.TryGet(out var m_Script))
            {
                if (m_Script.m_ClassName == "CubismMoc")
                {
                    moc = ParseMoc(monoBehaviour);
                    return true;
                }
            }
            moc = null;
            return false;
        }

        public static bool TryGetMocByHeader(MonoBehaviour monoBehaviour, out byte[] moc)
        {
            var reader = monoBehaviour.reader;
            reader.Reset();
            reader.Position += 28; //PPtr<GameObject> m_GameObject, m_Enabled, PPtr<MonoScript>
            reader.ReadAlignedString(); //m_Name
            var length = reader.ReadInt32();
            var signature = Encoding.UTF8.GetString(reader.ReadBytes(4)); //MOC3
            if (signature == "MOC3")
            {
                reader.Position -= 4;
                moc = reader.ReadBytes(length);
                return true;
            }
            moc = null;
            return false;
        }

        public static bool TryGetPhysicByClassName(MonoBehaviour monoBehaviour, out string physic)
        {
            if (monoBehaviour.m_Script.TryGet(out var m_Script))
            {
                if (m_Script.m_ClassName == "CubismPhysicsController")
                {
                    physic = ParsePhysics(monoBehaviour);
                    return true;
                }
            }
            physic = null;
            return false;
        }

        public static bool TryGetPhysicBySubRig(MonoBehaviour monoBehaviour, out string physic)
        {
            SerializedType serializedType = monoBehaviour.reader.serializedType;
            TypeTree type;
            if (serializedType?.m_Type != null)
            {
                type = serializedType.m_Type;
            }
            else
            {
                type = monoBehaviour.ToTypeTree();
            }
            TypeTreeNode withSubRig = type.m_Nodes.FirstOrDefault(x => x.m_Name == "SubRigs");//_rig;SubRigs
            if (withSubRig != null)
            {
                physic = ParsePhysics(monoBehaviour);
                return true;
            }
            physic = string.Empty;
            return false;
        }
    }
}