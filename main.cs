using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using dnlib.DotNet.Emit;

namespace Packer
{
    class Program
    {
        private static Random random = new Random();
        static void Main(string[] args)
        {
            Console.WriteLine("Packer v1.0\r\nGitHub: https://github.com/KrawkRE");
            Console.Write("Assembly: "); string dir = Console.ReadLine();
            string encrypted = Convert.ToBase64String(File.ReadAllBytes(dir));
            Run(encrypted, dir);
            Console.WriteLine("Sucessfully Packed !");
            Console.ReadKey();
        }
        public static string RandomString()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
            return new string(Enumerable.Repeat(chars, random.Next(5, 15))
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        //https://github.com/0xd4d/dnlib/blob/master/Examples/Example3.cs
        public static void Run(string encrypted, string writedir)
        {
            //create module
            var mod = new ModuleDefUser(RandomString()); mod.Kind = ModuleKind.Console;
            // create and add asm in module
            var asm = new AssemblyDefUser(RandomString(), new Version(random.Next(1, 9), random.Next(1, 9), random.Next(1, 9), random.Next(1, 9)));
            asm.Modules.Add(mod);
            // create startup class for ep
            var startUpType = new TypeDefUser(RandomString(), RandomString(), mod.CorLibTypes.Object.TypeDefOrRef);
            startUpType.Attributes = TypeAttributes.NotPublic | TypeAttributes.AutoLayout | TypeAttributes.Class | TypeAttributes.AnsiClass;
            mod.Types.Add(startUpType);
            //create ep method main(string[] args)
            var entryPoint = new MethodDefUser("Main", MethodSig.CreateStatic(mod.CorLibTypes.Void, new SZArraySig(mod.CorLibTypes.String)));
            entryPoint.Attributes = MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.ReuseSlot;
            entryPoint.ImplAttributes = MethodImplAttributes.IL | MethodImplAttributes.Managed;
            entryPoint.ParamDefs.Add(new ParamDefUser("args", 1));
            startUpType.Methods.Add(entryPoint);
            mod.EntryPoint = entryPoint;
            var epBody = new CilBody();
            entryPoint.Body = epBody;
            // add instructions in ep method
            epBody.Instructions.Add(OpCodes.Nop.ToInstruction());
            epBody.Instructions.Add(OpCodes.Ldstr.ToInstruction(encrypted));
            epBody.Instructions.Add(OpCodes.Call.ToInstruction(entryPoint.Module.Import(typeof(System.Convert).GetMethod("FromBase64String", new Type[] { typeof(string) }))));
            epBody.Instructions.Add(OpCodes.Call.ToInstruction(entryPoint.Module.Import(typeof(System.Reflection.Assembly).GetMethod("Load", new Type[] { typeof(byte[]) }))));
            epBody.Instructions.Add(OpCodes.Callvirt.ToInstruction(entryPoint.Module.Import(typeof(System.Reflection.Assembly).GetMethod("get_EntryPoint", new Type[0]))));
            epBody.Instructions.Add(OpCodes.Ldnull.ToInstruction());
            epBody.Instructions.Add(OpCodes.Ldc_I4_1.ToInstruction());
            epBody.Instructions.Add(OpCodes.Newarr.ToInstruction(entryPoint.Module.Import(typeof(System.Object))));
            epBody.Instructions.Add(OpCodes.Callvirt.ToInstruction(entryPoint.Module.Import(typeof(System.Reflection.MethodBase).GetMethod("Invoke", new Type[] { typeof(object), typeof(object[]) }))));
            epBody.Instructions.Add(OpCodes.Pop.ToInstruction());
            epBody.Instructions.Add(OpCodes.Ret.ToInstruction());
            // save new file
            mod.Write(writedir.Replace(".exe", "_packed.exe"));
        }
    }
}

