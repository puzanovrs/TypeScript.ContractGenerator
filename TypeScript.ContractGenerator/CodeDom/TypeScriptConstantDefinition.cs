using JetBrains.Annotations;

namespace SkbKontur.TypeScript.ContractGenerator.CodeDom
{
    public class TypeScriptConstantDefinition : TypeScriptStatement
    {
        public TypeScriptConstantDefinition([NotNull] string name, [NotNull] TypeScriptExpression value)
        {
            Name = name;
            Value = value;
        }
        
        public string Name { get; }
        public TypeScriptExpression Value { get; }

        public override string GenerateCode(ICodeGenerationContext context)
        {
            return $"const {Name} = {Value.GenerateCode(context)};";
        }
    }
}