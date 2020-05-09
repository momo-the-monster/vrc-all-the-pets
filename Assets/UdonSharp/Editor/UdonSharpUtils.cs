﻿
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UdonSharp
{
    public static class UdonSharpUtils
    {
        /// <summary>
        /// Apparently anything that takes a parameter is 5, and anything that doesn't is 1. So These are probably 1 byte per instruction, and 4 bytes per parameter
        /// So some day if the assembly is extended we might get 9 size instructions
        /// </summary>
        /// <param name="instruction"></param>
        /// <returns></returns>
        public static int GetUdonInstructionSize(string instruction)
        {
            switch (instruction)
            {
                case "LABEL": // The LABEL instruction gets replaced with a NOP by the end of compilation. It is just used to mark a target jump point
                case "NOP":
                case "POP":
                case "COPY":
                    return 4;
                case "PUSH":
                case "JUMP_IF_FALSE":
                case "JUMP":
                case "EXTERN":
                case "JUMP_INDIRECT":
                // The labeled variants of jump are fake and don't exist in Udon, 
                //  they get replaced towards the end of compilation with jumps to concrete addresses
                case "JUMP_LABEL":
                case "JUMP_IF_FALSE_LABEL":
                    return 8;
                case "ANNOTATION":
                    throw new System.NotImplementedException("ANNOTATION instruction is not yet implemented in Udon");
                default:
                    return 0;
            }
        }

        // https://stackoverflow.com/questions/6386202/get-type-name-without-any-generics-info
        public static string GetNameWithoutGenericArity(this System.Type t)
        {
            if (!t.IsGenericType)
                return t.Name;

            string name = t.Name;
            int index = name.IndexOf('`');
            return index == -1 ? name : name.Substring(0, index);
        }

        public static bool HasModifier(this SyntaxTokenList syntaxTokens, string modifier)
        {
            foreach (SyntaxToken token in syntaxTokens)
            {
                if (token.ValueText == modifier)
                    return true;
            }

            return false;
        }

        // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/conversions#implicit-numeric-conversions
        private static readonly IReadOnlyDictionary<System.Type, System.Type[]> implicitBuiltinConversions = new Dictionary<System.Type, System.Type[]>()
        {
            { typeof(sbyte), new System.Type[] { typeof(short), typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal) } },
            { typeof(byte), new System.Type[] { typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal) } },
            { typeof(short), new System.Type[] { typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal) } },
            { typeof(ushort), new System.Type[] { typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal) } },
            { typeof(int), new System.Type[] { typeof(long), typeof(float), typeof(double), typeof(decimal) } },
            { typeof(uint), new System.Type[] { typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal) } },
            { typeof(long), new System.Type[] { typeof(float), typeof(double), typeof(decimal) } },
            { typeof(ulong), new System.Type[] { typeof(float), typeof(double), typeof(decimal) } },
            { typeof(char), new System.Type[] { typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(float), typeof(decimal) } },
            { typeof(float), new System.Type[] { typeof(double) } },
        };

        private static readonly IReadOnlyDictionary<System.Type, System.Type> nextHighestPrecisionType = new Dictionary<System.Type, System.Type>()
        {
            { typeof(sbyte), typeof(int) },
            { typeof(byte), typeof(int) },
            { typeof(short), typeof(int) },
            { typeof(ushort), typeof(int) },
            { typeof(int), typeof(long) },
            { typeof(uint), typeof(long) },
        };

        private static readonly HashSet<System.Type> unsignedTypes = new HashSet<System.Type>()
        {
            typeof(byte),
            typeof(ushort),
            typeof(uint),
            typeof(ulong),
        };

        private static readonly HashSet<System.Type> signedTypes = new HashSet<System.Type>()
        {
            typeof(sbyte),
            typeof(short),
            typeof(int),
            typeof(long),
        };

        private static readonly HashSet<System.Type> integerTypes = new HashSet<System.Type>()
        {
            typeof(byte),
            typeof(sbyte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
        };

        private static readonly HashSet<System.Type> floatTypes = new HashSet<System.Type>()
        {
            typeof(float),
            typeof(double),
            typeof(decimal),
        };

        public static bool IsSignedType(System.Type type)
        {
            return signedTypes.Contains(type);
        }

        public static bool IsUnsignedType(System.Type type)
        {
            return unsignedTypes.Contains(type);
        }

        public static bool IsIntegerType(System.Type type)
        {
            return integerTypes.Contains(type);
        }

        public static bool IsFloatType(System.Type type)
        {
            return floatTypes.Contains(type);
        }

        public static bool IsNumericType(System.Type type)
        {
            return implicitBuiltinConversions.ContainsKey(type);
        }

        public static bool IsNumericImplicitCastValid(System.Type targetType, System.Type sourceType)
        {
            if (implicitBuiltinConversions.ContainsKey(sourceType) && implicitBuiltinConversions[sourceType].Contains(targetType))
                return true;

            return false;
        }

        public static System.Type GetNextHighestNumericPrecision(System.Type type)
        {
            if (type == null)
                return null;

            System.Type precisionType = null;
            nextHighestPrecisionType.TryGetValue(type, out precisionType);

            return precisionType;
        }

        public static MethodInfo GetNumericConversionMethod(System.Type targetType, System.Type sourceType)
        {
            IEnumerable<MethodInfo> foundMethods = typeof(System.Convert)
                .GetMethods(BindingFlags.Static | BindingFlags.Public)
                .Where(e => e.Name == $"To{targetType.Name}")
                .Where(e => e.GetParameters().FirstOrDefault().ParameterType == sourceType);

            if (sourceType.IsEnum)
            {
                foundMethods = typeof(System.Convert).GetMethods(BindingFlags.Static | BindingFlags.Public)
                                                     .Where(e => e.Name == $"To{targetType.Name}")
                                                     .Where(e => e.GetParameters().FirstOrDefault().ParameterType == typeof(object));
            }

            return foundMethods.FirstOrDefault();
        }

        public static bool IsNumericExplicitCastValid(System.Type targetType, System.Type sourceType)
        {
            return IsNumericType(sourceType) && GetNumericConversionMethod(targetType, sourceType) != null;
        }

        public static bool IsImplicitlyAssignableFrom(this System.Type targetType, System.Type assignee)
        {
            // Normal explicit assign
            if (targetType.IsAssignableFrom(assignee))
                return true;

            // Implicit numeric conversions
            if (IsNumericImplicitCastValid(targetType, assignee))
                return true;

            // Handle user-defined implicit conversion operators defined on both sides
            // Roughly follows https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/conversions#processing-of-user-defined-implicit-conversions

            // I doubt I'll ever deal with properly supporting nullable but ¯\_(ツ)_/¯
            if (System.Nullable.GetUnderlyingType(targetType) != null)
                targetType = System.Nullable.GetUnderlyingType(targetType);
            if (System.Nullable.GetUnderlyingType(assignee) != null)
                assignee = System.Nullable.GetUnderlyingType(assignee);

            List<System.Type> operatorTypes = new List<System.Type>();
            operatorTypes.Add(targetType);

            System.Type currentSourceType = assignee;
            while(currentSourceType != null)
            {
                operatorTypes.Add(currentSourceType);
                currentSourceType = currentSourceType.BaseType;
            }

            foreach (System.Type operatorType in operatorTypes)
            {
                IEnumerable<MethodInfo> methods = operatorType.GetMethods(BindingFlags.Public | BindingFlags.Static).Where(e => e.Name == "op_Implicit");

                foreach (MethodInfo methodInfo in methods)
                {
                    if (methodInfo.ReturnType == targetType && (methodInfo.GetParameters()[0].ParameterType == assignee || methodInfo.GetParameters()[0].ParameterType == typeof(UnityEngine.Object)))
                        return true;
                }
            }

            return false;
        }

        public static bool IsValidNumericImplictCastSourceType(this System.Type sourceType)
        {
            return implicitBuiltinConversions.ContainsKey(sourceType);
        }

        public static bool IsValidNumericImplicitCastTargetType(this System.Type targetType)
        {
            foreach (var lookupKeyVal in implicitBuiltinConversions)
            {
                foreach (System.Type testTargetType in lookupKeyVal.Value)
                {
                    if (targetType == testTargetType)
                        return true;
                }
            }

            return false;
        }

        public static int GetImplicitNumericCastDistance(System.Type targetType, System.Type sourceType)
        {
            if (targetType == sourceType)
                return 0;

            System.Type[] targetTypes;

            if (!implicitBuiltinConversions.TryGetValue(sourceType, out targetTypes))
            {
                throw new System.ArgumentException("Could not find a implicit numeric cast for the source type");
            }

            for (int i = 0; i < targetTypes.Length; ++i)
            {
                if (targetTypes[i] == targetType)
                    return i + 1;
            }

            throw new System.ArgumentException("Could not find applicable cast for target type");
        }

        public static bool HasParamsParameter(this ParameterInfo parameterInfo)
        {
            return parameterInfo.GetCustomAttributes(typeof(System.ParamArrayAttribute), false).Length > 0;
        }
        
        // https://stackoverflow.com/questions/4168489/methodinfo-equality-for-declaring-type
        public static bool AreMethodsEqualForDeclaringType(this MethodBase first, MethodBase second)
        {
            first = first.ReflectedType == first.DeclaringType ? first : first.DeclaringType.GetMethod(first.Name, first.GetParameters().Select(p => p.ParameterType).ToArray());
            second = second.ReflectedType == second.DeclaringType ? second : second.DeclaringType.GetMethod(second.Name, second.GetParameters().Select(p => p.ParameterType).ToArray());

            // Special case for comparing object functions since they need to be explicitly included in GetMethods
            if (first.DeclaringType == typeof(object))
                second = typeof(object).GetMethod(second.Name, second.GetParameters().Select(p => p.ParameterType).ToArray());
            if (second.DeclaringType == typeof(object))
                first = typeof(object).GetMethod(first.Name, first.GetParameters().Select(p => p.ParameterType).ToArray());

            return first == second;
        }

        private static readonly HashSet<System.Type> udonSyncTypes = new HashSet<System.Type>()
        {
            typeof(bool),
            typeof(char),
            typeof(byte), typeof(sbyte),
            typeof(int), typeof(uint),
            typeof(long), typeof(ulong),
            typeof(float), typeof(double),
            typeof(short), typeof(ushort),
            typeof(string),
            typeof(UnityEngine.Vector2), typeof(UnityEngine.Vector3), typeof(UnityEngine.Vector4),
            typeof(UnityEngine.Quaternion),
            typeof(UnityEngine.Color32), typeof(UnityEngine.Color),
        };

        public static bool IsUdonSyncedType(System.Type type)
        {
            return udonSyncTypes.Contains(type);
        }

        private static readonly HashSet<System.Type> builtinTypes = new HashSet<System.Type>
        {
            typeof(string),
            typeof(bool),
            typeof(byte),
            typeof(sbyte),
            typeof(char),
            typeof(decimal),
            typeof(double),
            typeof(float),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(short),
            typeof(ushort),
            typeof(object),
        };

        public static bool IsBuiltinType(System.Type type)
        {
            return builtinTypes.Contains(type);
        }

        public static MethodInfo[] GetOperators(System.Type type, BuiltinOperatorType builtinOperatorType)
        {
            List<MethodInfo> foundOperators = new List<MethodInfo>();

            // If it's a builtin type then create a fake operator methodinfo for it.
            // If this operator doesn't actually exist, it will get filtered by the overload finding
            if (IsBuiltinType(type))
                foundOperators.Add(new OperatorMethodInfo(type, builtinOperatorType));

            // Now look for operators that the type defines
            string operatorName = System.Enum.GetName(typeof(BuiltinOperatorType), builtinOperatorType);
            if (builtinOperatorType == BuiltinOperatorType.Multiplication)
                operatorName = "Multiply"; // Udon breaks standard naming with its multiplication overrides on base types
            else if (builtinOperatorType == BuiltinOperatorType.UnaryMinus)
                operatorName = "UnaryNegation";

            operatorName = $"op_{operatorName}";

            System.Type currentType = type;

            while (currentType != null)
            {
                foundOperators.AddRange(currentType.GetMethods(BindingFlags.Public | BindingFlags.Static).Where(e => e.Name == operatorName));
                currentType = currentType.BaseType;
            }

            // Add the object equality and inequality operators if we haven't already found better matches
            if (foundOperators.Count == 0 && type != typeof(object) && !type.IsValueType &&
                (builtinOperatorType == BuiltinOperatorType.Equality || builtinOperatorType == BuiltinOperatorType.Inequality))
                foundOperators.AddRange(GetOperators(typeof(object), builtinOperatorType));
            else if (foundOperators.Count == 0 && type.IsEnum && (builtinOperatorType == BuiltinOperatorType.Equality || builtinOperatorType == BuiltinOperatorType.Inequality)) // Handle enum comparisons
                foundOperators.Add(typeof(object).GetMethod("Equals", BindingFlags.Public | BindingFlags.Static));

            return foundOperators.ToArray();
        }
        
        public static string PrettifyTypeName(System.Type type)
        {
            if (type == typeof(sbyte))
                return "sbyte";
            else if (type == typeof(byte))
                return "byte";
            else if (type == typeof(short))
                return "short";
            else if (type == typeof(ushort))
                return "ushort";
            else if (type == typeof(int))
                return "int";
            else if (type == typeof(uint))
                return "uint";
            else if (type == typeof(long))
                return "long";
            else if (type == typeof(ulong))
                return "ulong";
            else if (type == typeof(char))
                return "char";
            else if (type == typeof(string))
                return "string";
            else if (type == typeof(float))
                return "float";
            else if (type == typeof(double))
                return "double";
            else if (type == typeof(bool))
                return "bool";
            else
                return type.Name;
        }

        public static bool IsUserDefinedBehaviour(System.Type type)
        {
            return type == typeof(UdonSharpBehaviour) ||
                   type.IsSubclassOf(typeof(UdonSharpBehaviour)) ||
                  (type.IsArray && (type.GetElementType().IsSubclassOf(typeof(UdonSharpBehaviour)) || type.GetElementType() == typeof(UdonSharpBehaviour)));
        }
        
        public static bool IsUserJaggedArray(System.Type type)
        {
            return type.IsArray && type.GetElementType().IsArray;
        }

        public static bool IsUserDefinedType(System.Type type)
        {
            return IsUserDefinedBehaviour(type) ||
                   IsUserJaggedArray(type);
        }

        private static Dictionary<System.Type, System.Type> userTypeToUdonTypeCache = new Dictionary<System.Type, System.Type>();

        public static System.Type UserTypeToUdonType(System.Type type)
        {
            System.Type udonType;
            if (!userTypeToUdonTypeCache.TryGetValue(type, out udonType))
            {
                if (IsUserDefinedType(type))
                {
                    if (type.IsArray)
                    {
                        if (!type.GetElementType().IsArray)
                        {
                            udonType = typeof(UnityEngine.Component[]);// Hack because VRC doesn't expose the array type of UdonBehaviour
                        }
                        else // Jagged arrays
                        {
                            udonType = typeof(object[]);
                        }
                    }
                    else
                    {
                        udonType = typeof(VRC.Udon.UdonBehaviour);
                    }
                }

                if (udonType == null)
                    udonType = type;

                userTypeToUdonTypeCache.Add(type, udonType);
            }

            return udonType;
        }

        public static string LogBuildError(string message, string filePath, int line, int character)
        {
            MethodInfo buildErrorLogMethod = typeof(UnityEngine.Debug).GetMethod("LogPlayerBuildError", BindingFlags.NonPublic | BindingFlags.Static);

            string errorMessage = $"[<color=#FF00FF>UdonSharp</color>] {filePath}({line + 1},{character}): {message}";

            buildErrorLogMethod.Invoke(null, new object[] {
                        errorMessage,
                        filePath,
                        line + 1,
                        character });

            return errorMessage;
        }

        public static string ReadFileTextSync(string filePath, float timeoutSeconds = 2f)
        {
            bool sourceLoaded = false;

            string fileText = "";

            System.DateTime startTime = System.DateTime.Now;

            while (true)
            {
                System.IO.IOException exception = null;

                try
                {
                    fileText = System.IO.File.ReadAllText(filePath);
                    sourceLoaded = true;
                }
                catch (System.IO.IOException e)
                {
                    exception = e;
                }

                if (sourceLoaded)
                    break;
                else
                    System.Threading.Thread.Sleep(20);

                // 2 second timeout
                System.TimeSpan timeFromStart = System.DateTime.Now - startTime;

                if (timeFromStart.TotalSeconds > timeoutSeconds)
                {
                    UnityEngine.Debug.LogError("Timeout when attempting to read modified C# source file");
                    if (exception != null)
                        throw exception;
                }
            }

            return fileText;
        }
    }
}