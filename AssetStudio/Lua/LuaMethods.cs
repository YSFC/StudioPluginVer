using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace AssetStudio;

public class LuaMethods
{
    public Delegate GetAnyMethod(string className, string methodName, object target = null)
    {
        var type = Type.GetType(className);
        if (type == null)
        {
            throw new Exception($"Class {className} not found.");
        }

        var method = type.GetMethod(methodName);
        if (method == null)
        {
            throw new Exception($"Method {methodName} not found in class {className}.");
        }

        var delegateType = GetDelegateType(method);
        var del = Delegate.CreateDelegate(delegateType, target, method);

        return del;
    }

    private Type GetDelegateType(MethodInfo methodInfo)
    {
        var types = methodInfo.GetParameters().Select(p => p.ParameterType).ToList();
        if (methodInfo.ReturnType != typeof(void))
        {
            types.Add(methodInfo.ReturnType);
        }
        return Expression.GetDelegateType(types.ToArray());
    }

    #region IO
    
    public byte[] ReadAllBytes(string path)
    {
        return File.ReadAllBytes(path);
    }
    
    public void WriteAllBytes(string path, byte[] bytes)
    {
        File.WriteAllBytes(path, bytes);
    }
    
    public FileStream CreateFileStream(string path)
    {
        return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    }

    public MemoryStream CreateMemoryStream()
    {
        return new MemoryStream();
    }
    
    public MemoryStream CreateMemoryStreamFormByteArray(byte[] src)
    {
        return new MemoryStream(src);
    }
    
    #endregion

    #region DataReader
    
    public EndianBinaryReader CreateEndianBinaryReader(Stream stream)
    {
        return new EndianBinaryReader(stream);
    }
    
    public byte[] SliceByteArray(byte[] src, int start, int length)
    {
        var buffer = new byte[length];
        Array.Copy(src, start, buffer, 0, length);
        return buffer;
    }
    
    public byte[] EncodeStringToBytes(string src)
    {
        return Encoding.UTF8.GetBytes(src);
    }
    
    #endregion

    #region Logger
    
    public void Verbose(string message)
    {
        Logger.Verbose(message);
    }
    
    public void Error(string message)
    {
        Logger.Error(message);
    }
    
    public void Debug(string message)
    {
        Logger.Debug(message);
    }
    
    public void Info(string message)
    {
        Logger.Info(message);
    }
    
    public void Warning(string message)
    {
        Logger.Warning(message);
    }
    
    #endregion

    #region AES

    public static byte[] AES_Decrypt(byte[] data, byte[] key, byte[] iv)
    {
        using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
        {
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using (ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
            {
                return PerformCryptography(data, decryptor);
            }
        }
    }

    private static byte[] PerformCryptography(byte[] data, ICryptoTransform cryptoTransform)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            using (CryptoStream cs = new CryptoStream(ms, cryptoTransform, CryptoStreamMode.Write))
            {
                cs.Write(data, 0, data.Length);
                cs.FlushFinalBlock();
                return ms.ToArray();
            }
        }
    }

    #endregion
}