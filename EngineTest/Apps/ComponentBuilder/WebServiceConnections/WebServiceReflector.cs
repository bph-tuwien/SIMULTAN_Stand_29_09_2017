using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.InteropServices;

using WebServiceConnector.ShadowService;

namespace ComponentBuilder.WebServiceConnections
{
    public class WebServiceReflector
    {


        public static string GetModuleInfo(string _module_name)
        {
            if (string.IsNullOrEmpty(_module_name)) return string.Empty;

            return string.Empty;
        }

        public static string GetAssemblyInfo(Type _type)
        {
            if (_type == null) return string.Empty;

            Assembly a = _type.Assembly;
            return a.FullName;
        }

        public static List<string> GetAsseblyTypeNames(Type _type)
        {
            if (_type == null) return new List<string>();

            Assembly a = _type.Assembly;
            if (a == null) return new List<string>();

            Type[] a_types = a.GetTypes();
            if (a_types != null && a_types.Length > 0)
                return a_types.Select(x => x.FullName).ToList();
            else
                return new List<string>();
        }

        public static List<Type> GetServiceTypesInAssemblyOf(Type _any_type, string _contained_in_type_name)
        {
            if (_any_type == null) return null;

            Assembly a = _any_type.Assembly;
            if (a == null) return null;

            Type[] a_types = a.GetTypes();
            if (a_types != null && a_types.Length > 0)
            {
                List<Type> possible_service_type = a_types.Where(x => x.Name.Contains(_contained_in_type_name)).ToList();
                return possible_service_type;
            }
            return null;
        }

        public static MethodInfo GetMethodByName(Type _any_type, string _contained_in_method_name)
        {
            if (_any_type == null || string.IsNullOrEmpty(_contained_in_method_name))
                return null;

            List<MethodInfo> all = _any_type.GetMethods().ToList();
            MethodInfo match = null;
            foreach(MethodInfo mi in all)
            {
                if (mi.Name.Contains(_contained_in_method_name))
                {
                    // CHECKS:
                    // 1. CallingConverntion (HasThis for instance, Standard for static methods)
                    if (!mi.CallingConvention.HasFlag(CallingConventions.HasThis)) continue;
                    // 2. IsAbstract (cannot be called!)
                    if (mi.IsAbstract) continue;
                    // 3. IsConstructor (should not be called)
                    if (mi.IsConstructor) continue;
                    // 4. IsGeneric (should not be the case)
                    if (mi.IsGenericMethod) continue;
                    if (mi.IsGenericMethodDefinition) continue;
                    // 5. IsPublic (should apply)
                    if (!mi.IsPublic) continue;
                    // 6. IsStatic (should NOT apply)
                    if (mi.IsStatic) continue;
                    // 7. MemberType (sould be Method)
                    if (mi.MemberType != MemberTypes.Method) continue;                   

                    match = mi;
                    break;
                }                   
            }

            return match;
        }

        public static ConstructorInfo[] GetCtorInfoFor(Type _type)
        {
            if (_type == null) return null;

            ConstructorInfo[] all = _type.GetConstructors();
            return all;
        }

        public static void GetTypeInfo(Type _type)
        {
            // get the constructors with parameters

        }
    }
}
