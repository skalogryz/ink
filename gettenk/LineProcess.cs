using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace gettenk
{
    public class LinesProcess
    {
        public virtual string LineForPot(string s)
        {
            return s;
        }
        public virtual string OptValForPot(string s)
        {
            return s;
        }
    }

    public class DllProcess : LinesProcess
    {
        public string dll = "";
        public string optvalfn = "";
        Assembly asm = null;
        MethodInfo method = null;

        public void LoadAsm()
        {
            if (asm != null) return;
            Assembly asmlow = Assembly.LoadFile(dll);
            string[] n = optvalfn.Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries);
            if (n.Length < 2) return;
            string ns;
            string tp;
            string m;
            Type type = null;
            if (n.Length <= 2)
            {
                ns = string.Empty;
                tp = n[0];
                m = n[1];
            } else
            {
                ns = n[0];
                tp = n[1];
                m = n[2];
            }
            Type[] tlist = asmlow.GetTypes();
            foreach (Type t in tlist)
            {
                if (!string.IsNullOrEmpty(ns))
                {
                    if (t.Namespace != ns) 
                        continue;
                }
                if (t.Name == tp)
                {
                    type = t;
                    break;
                }

            }

            if (type == null) return;

            MethodInfo mm = type.GetMethod(m);
            if ((mm != null) && (mm.IsStatic))
            {
                method = mm as MethodInfo;
                asm = asmlow;
            }

        }

        public override string OptValForPot(string s)
        {
            if (asm == null) return base.OptValForPot(s);

            string res = method.Invoke(null, new object[] { s }) as string;
            if (res is null) return string.Empty;
            return res;
        }
    }
}
