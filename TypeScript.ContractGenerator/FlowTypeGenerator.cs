using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using TypeScript.CodeDom;
using TypeScript.ContractGenerator.Internals;
using TypeScript.ContractGenerator.TypeBuilders;

namespace TypeScript.ContractGenerator
{
    public class FlowTypeGenerator : ITypeGenerator
    {
        public FlowTypeGenerator(ICustomTypeGenerator customTypeGenerator, Type[] rootTypes)
        {
            this.rootTypes = rootTypes;
            flowTypeUnitFactory = new DefaultFlowTypeGeneratorOutput();
            this.customTypeGenerator = customTypeGenerator ?? new NullCustomTypeGenerator();
            flowTypeDeclarations = new Dictionary<Type, ITypeBuildingContext>();
        }

        public FlowTypeUnit[] Generate()
        {
            foreach (var type in rootTypes)
                RequestTypeBuild(type);
            while (flowTypeDeclarations.Values.Any(x => !x.IsDefinitionBuilded))
            {
                foreach (var currentType in flowTypeDeclarations.ToArray())
                {
                    if (!currentType.Value.IsDefinitionBuilded)
                        currentType.Value.BuildDefiniion(this);
                }
            }
            return flowTypeUnitFactory.Units;
        }

        public void GenerateFiles(string targetPath)
        {
            foreach (var type in rootTypes)
                RequestTypeBuild(type);
            while (flowTypeDeclarations.Values.Any(x => !x.IsDefinitionBuilded))
            {
                foreach (var currentType in flowTypeDeclarations.ToArray())
                {
                    if (!currentType.Value.IsDefinitionBuilded)
                        currentType.Value.BuildDefiniion(this);
                }
            }
            FilesGenerator.GenerateFiles(targetPath, flowTypeUnitFactory);
        }

        public void GenerateTypeScriptFiles(string targetPath)
        {
            foreach (var type in rootTypes)
                RequestTypeBuild(type);
            while (flowTypeDeclarations.Values.Any(x => !x.IsDefinitionBuilded))
            {
                foreach (var currentType in flowTypeDeclarations.ToArray())
                {
                    if (!currentType.Value.IsDefinitionBuilded)
                        currentType.Value.BuildDefiniion(this);
                }
            }
            FilesGenerator.DeleteFiles(targetPath, "*.js");
            FilesGenerator.DeleteFiles(targetPath, "*.tsx");
            FilesGenerator.GenerateTypeScriptFiles(targetPath, flowTypeUnitFactory);
        }

        private void RequestTypeBuild(Type type)
        {
            ResolveType(type);
        }

        public ITypeBuildingContext ResolveType(Type type)
        {
            if (flowTypeDeclarations.ContainsKey(type))
            {
                return flowTypeDeclarations[type];
            }
            var typeLocation = customTypeGenerator.GetTypeLocation(type);
            var typeBuildingContext = customTypeGenerator.ResolveType(typeLocation, type, flowTypeUnitFactory);
            if (typeBuildingContext == null)
            {
                if (BuildInTypeBuildingContext.Accept(type))
                {
                    typeBuildingContext = new BuildInTypeBuildingContext(type);
                }
                if (type.IsArray)
                {
                    typeBuildingContext = new ArrayTypeBuildingContext(type.GetElementType());
                }
                if (type.IsEnum)
                {
                    var targetUnit = flowTypeUnitFactory.GetOrCreateTypeUnit(typeLocation);
                    typeBuildingContext = new EnumTypeBuildingContextImpl(targetUnit, type);
                }
                if (type.IsGenericType && !type.IsGenericTypeDefinition)
                {
                    typeBuildingContext = new GenericTypeTypeBuildingContextImpl(type);
                }
                if (type.IsGenericParameter)
                {
                    typeBuildingContext = new GenericParameterTypeBuildingContextImpl(type);
                }
                if (type.IsGenericTypeDefinition)
                {
                    var targetUnit = flowTypeUnitFactory.GetOrCreateTypeUnit(typeLocation);
                    typeBuildingContext = new CustomTypeTypeBuildingContextImpl(targetUnit, type);
                }
                if (typeBuildingContext == null)
                {
                    var targetUnit = flowTypeUnitFactory.GetOrCreateTypeUnit(typeLocation);
                    typeBuildingContext = new CustomTypeTypeBuildingContextImpl(targetUnit, type);
                }
            }
            typeBuildingContext.Initialize(this);
            flowTypeDeclarations.Add(type, typeBuildingContext);
            return typeBuildingContext;
        }

        public FlowTypeType BuildAndImportType(FlowTypeUnit targetUnit, ICustomAttributeProvider attributeProvider, Type type)
        {
            var (isNullable, resultType) = FlowTypeGeneratorHelpers.ProcessNullable(attributeProvider, type);
            var result = GetFlowTypeType(targetUnit, resultType);
            if (isNullable)
                result = new FlowTypeNullableType(result);
            return result;
        }

        private FlowTypeType GetFlowTypeType(FlowTypeUnit targetUnit, Type type)
        {
            if (flowTypeDeclarations.ContainsKey(type))
                return flowTypeDeclarations[type].ReferenceFrom(targetUnit, this);
            if (type.IsGenericTypeDefinition && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                return new FlowTypeNullableType(GetFlowTypeType(targetUnit, type.GetGenericArguments()[0]));
            var context = ResolveType(type);
            return context.ReferenceFrom(targetUnit, this);
        }

        private readonly Type[] rootTypes;
        private readonly DefaultFlowTypeGeneratorOutput flowTypeUnitFactory;
        private readonly ICustomTypeGenerator customTypeGenerator;
        private readonly Dictionary<Type, ITypeBuildingContext> flowTypeDeclarations;
    }
}