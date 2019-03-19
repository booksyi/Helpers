using AutoMapper;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HelpersForCore
{
    public class CodingMapper
    {

    }

    public class CodingMapperProfile : Profile
    {
        public CodingMapperProfile()
        {
            // Schema To Syntax
            CreateMap<CsSchema.Access, IEnumerable<SyntaxToken>>().ConvertUsing<CsAccessToSyntaxTokensConverter>();
            CreateMap<CsSchema.Unit, CompilationUnitSyntax>().ConvertUsing<CsUnitToSyntaxConverter>();
            CreateMap<CsSchema.Namespace, NamespaceDeclarationSyntax>().ConvertUsing<CsNamespaceToSyntaxConverter>();
            CreateMap<CsSchema.Class, ClassDeclarationSyntax>().ConvertUsing<CsClassToSyntaxConverter>();
            CreateMap<CsSchema.Field, FieldDeclarationSyntax>().ConvertUsing<CsFieldToSyntaxConverter>();
            CreateMap<CsSchema.Property, PropertyDeclarationSyntax>().ConvertUsing<CsPropertyToSyntaxConverter>();
            CreateMap<CsSchema.Attribute, AttributeSyntax>().ConvertUsing<CsAttributeToSyntaxConverter>();
            // Schema To Schema
            CreateMap<DbSchema.Table, CsSchema.Class>().ConvertUsing<DbTableToCsClassConverter>();
            CreateMap<DbSchema.Field, CsSchema.Property>().ConvertUsing<DbFieldToCsPropertyConverter>();
            CreateMap<CsSchema.Class, TsSchema.Class>().ConvertUsing<CsClassToTsClassConverter>();
            CreateMap<CsSchema.Property, TsSchema.Property>().ConvertUsing<CsPropertyToTsPropertyConverter>();
        }
    }

    #region Schema To Syntax Converters
    public class CsAccessToSyntaxTokensConverter : ITypeConverter<CsSchema.Access, IEnumerable<SyntaxToken>>
    {
        public IEnumerable<SyntaxToken> Convert(CsSchema.Access source, IEnumerable<SyntaxToken> destination, ResolutionContext context)
        {
            switch (source)
            {
                case CsSchema.Access.Public:
                    yield return SyntaxFactory.Token(SyntaxKind.PublicKeyword);
                    break;
                case CsSchema.Access.Protected:
                    yield return SyntaxFactory.Token(SyntaxKind.ProtectedKeyword);
                    break;
                case CsSchema.Access.Internal:
                    yield return SyntaxFactory.Token(SyntaxKind.InternalKeyword);
                    break;
                case CsSchema.Access.ProtectedInternal:
                    yield return SyntaxFactory.Token(SyntaxKind.ProtectedKeyword);
                    yield return SyntaxFactory.Token(SyntaxKind.InternalKeyword);
                    break;
                case CsSchema.Access.Private:
                    yield return SyntaxFactory.Token(SyntaxKind.PrivateKeyword);
                    break;
                case CsSchema.Access.PrivateProtected:
                    yield return SyntaxFactory.Token(SyntaxKind.PrivateKeyword);
                    yield return SyntaxFactory.Token(SyntaxKind.ProtectedKeyword);
                    break;
            }
        }
    }

    public class CsUnitToSyntaxConverter : ITypeConverter<CsSchema.Unit, CompilationUnitSyntax>
    {
        private readonly IMapper mapper;
        public CsUnitToSyntaxConverter(IMapper mapper)
        {
            this.mapper = mapper;
        }

        public CompilationUnitSyntax Convert(CsSchema.Unit source, CompilationUnitSyntax destination, ResolutionContext context)
        {
            var syntax = SyntaxFactory.CompilationUnit();

            if (source.Usings != null)
            {
                foreach (string @using in source.Usings)
                {
                    syntax = syntax.AddUsings(
                        SyntaxFactory.UsingDirective(
                            SyntaxFactory.ParseName(@using)));
                }
            }
            if (source.Namespaces != null)
            {
                foreach (var @namespace in source.Namespaces)
                {
                    if (string.IsNullOrWhiteSpace(@namespace.Name) == false)
                    {
                        syntax = syntax.AddMembers(mapper.Map<NamespaceDeclarationSyntax>(@namespace));
                    }
                    else
                    {
                        syntax = syntax.AddMembers(mapper.Map<ClassDeclarationSyntax[]>(@namespace.Classes));
                    }
                }
            }
            return syntax;
        }
    }

    public class CsNamespaceToSyntaxConverter : ITypeConverter<CsSchema.Namespace, NamespaceDeclarationSyntax>
    {
        private readonly IMapper mapper;
        public CsNamespaceToSyntaxConverter(IMapper mapper)
        {
            this.mapper = mapper;
        }

        public NamespaceDeclarationSyntax Convert(CsSchema.Namespace source, NamespaceDeclarationSyntax destination, ResolutionContext context)
        {
            var syntax = SyntaxFactory.NamespaceDeclaration(
                SyntaxFactory.ParseName(source.Name)).NormalizeWhitespace();
            if (source.Classes != null)
            {
                foreach (CsSchema.Class @class in source.Classes)
                {
                    syntax = syntax.AddMembers(mapper.Map<ClassDeclarationSyntax>(@class));
                }
            }
            return syntax;
        }
    }

    public class CsClassToSyntaxConverter : ITypeConverter<CsSchema.Class, ClassDeclarationSyntax>
    {
        private readonly IMapper mapper;
        public CsClassToSyntaxConverter(IMapper mapper)
        {
            this.mapper = mapper;
        }

        public ClassDeclarationSyntax Convert(CsSchema.Class source, ClassDeclarationSyntax destination, ResolutionContext context)
        {
            var syntax = SyntaxFactory.ClassDeclaration(source.Name)
                .AddModifiers(mapper.Map<IEnumerable<SyntaxToken>>(source.Access).ToArray())
                .NormalizeWhitespace();
            if (source.InheritTypeNames != null)
            {
                foreach (string inherit in source.InheritTypeNames)
                {
                    syntax = syntax.AddBaseListTypes(
                        SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(inherit)));
                }
            }
            if (source.Attributes != null)
            {
                syntax = syntax.AddAttributeLists(
                    SyntaxFactory.AttributeList(
                        new SeparatedSyntaxList<AttributeSyntax>().AddRange(
                            mapper.Map<AttributeSyntax[]>(source.Attributes))));
            }
            if (source.Fields != null)
            {
                foreach (CsSchema.Field field in source.Fields)
                {
                    syntax = syntax.AddMembers(mapper.Map<FieldDeclarationSyntax>(field));
                }
            }
            if (source.Properties != null)
            {
                foreach (CsSchema.Property property in source.Properties)
                {
                    syntax = syntax.AddMembers(mapper.Map<PropertyDeclarationSyntax>(property));
                }
            }
            if (source.Classes != null)
            {
                foreach (CsSchema.Class @class in source.Classes)
                {
                    syntax = syntax.AddMembers(mapper.Map<ClassDeclarationSyntax>(@class));
                }
            }
            return syntax;
        }
    }

    public class CsFieldToSyntaxConverter : ITypeConverter<CsSchema.Field, FieldDeclarationSyntax>
    {
        private readonly IMapper mapper;
        public CsFieldToSyntaxConverter(IMapper mapper)
        {
            this.mapper = mapper;
        }

        public FieldDeclarationSyntax Convert(CsSchema.Field source, FieldDeclarationSyntax destination, ResolutionContext context)
        {
            var variableDeclaration = SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName(source.TypeName))
                .AddVariables(SyntaxFactory.VariableDeclarator(source.Name));
            var syntax = SyntaxFactory.FieldDeclaration(variableDeclaration)
                .AddModifiers(mapper.Map<IEnumerable<SyntaxToken>>(source.Access).ToArray());
            if (source.Attributes != null)
            {
                syntax = syntax.AddAttributeLists(
                    SyntaxFactory.AttributeList(
                        new SeparatedSyntaxList<AttributeSyntax>().AddRange(
                            mapper.Map<AttributeSyntax[]>(source.Attributes))));
            }
            return syntax;
        }
    }

    public class CsPropertyToSyntaxConverter : ITypeConverter<CsSchema.Property, PropertyDeclarationSyntax>
    {
        private readonly IMapper mapper;
        public CsPropertyToSyntaxConverter(IMapper mapper)
        {
            this.mapper = mapper;
        }

        public PropertyDeclarationSyntax Convert(CsSchema.Property source, PropertyDeclarationSyntax destination, ResolutionContext context)
        {
            var syntax = SyntaxFactory.PropertyDeclaration(
                SyntaxFactory.ParseTypeName(source.TypeName), source.Name)
                    .AddModifiers(mapper.Map<IEnumerable<SyntaxToken>>(source.Access).ToArray())
                    .AddAccessorListAccessors(
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));
            if (source.Attributes != null)
            {
                syntax = syntax.AddAttributeLists(
                    SyntaxFactory.AttributeList(
                        new SeparatedSyntaxList<AttributeSyntax>().AddRange(
                            mapper.Map<AttributeSyntax[]>(source.Attributes))));
            }
            return syntax;
        }
    }

    public class CsAttributeToSyntaxConverter : ITypeConverter<CsSchema.Attribute, AttributeSyntax>
    {
        private readonly IMapper mapper;
        public CsAttributeToSyntaxConverter(IMapper mapper)
        {
            this.mapper = mapper;
        }

        public AttributeSyntax Convert(CsSchema.Attribute source, AttributeSyntax destination, ResolutionContext context)
        {
            var syntax = SyntaxFactory.Attribute(
                SyntaxFactory.ParseName(source.Name));
            if (source.ArgumentExpressions != null)
            {
                foreach (var expression in source.ArgumentExpressions)
                {
                    var argument = SyntaxFactory.AttributeArgument(SyntaxFactory.ParseExpression(expression));
                    syntax = syntax.AddArgumentListArguments(argument);
                }
            }
            return syntax;
        }
    }
    #endregion
    #region Schema To Schema Converters
    public class DbTableToCsClassConverter : ITypeConverter<DbSchema.Table, CsSchema.Class>
    {
        private readonly IMapper mapper;
        public DbTableToCsClassConverter(IMapper mapper)
        {
            this.mapper = mapper;
        }

        public CsSchema.Class Convert(DbSchema.Table source, CsSchema.Class destination, ResolutionContext context)
        {
            CsSchema.Class @class = new CsSchema.Class();
            @class.Name = source.Name.UpperFirst();
            @class.Properties = mapper.Map<CsSchema.Property[]>(source.Fields);
            return @class;
        }
    }

    public class DbFieldToCsPropertyConverter : ITypeConverter<DbSchema.Field, CsSchema.Property>
    {
        public CsSchema.Property Convert(DbSchema.Field source, CsSchema.Property destination, ResolutionContext context)
        {
            CsSchema.Property property = new CsSchema.Property() { Name = source.Name };
            #region set attributes
            List<CsSchema.Attribute> attributes = new List<CsSchema.Attribute>();
            if (source.IsIdentity)
            {
                attributes.Add(new CsSchema.Attribute("Key"));
                attributes.Add(new CsSchema.Attribute("DatabaseGenerated")
                {
                    ArgumentExpressions = new string[]
                    {
                        "DatabaseGeneratedOption.Identity"
                    }
                });
            }
            if (source.TypeName.In("char", "nchar", "ntext", "nvarchar", "text", "varchar", "xml"))
            {
                if (source.IsNullable == false)
                {
                    attributes.Add(new CsSchema.Attribute("Required"));
                }
                attributes.Add(new CsSchema.Attribute("Column")
                {
                    ArgumentExpressions = new string[]
                    {
                        $"\"{source.Name}\""
                    }
                });
                if (source.Length > 0)
                {
                    attributes.Add(new CsSchema.Attribute("StringLength")
                    {
                        ArgumentExpressions = new string[]
                        {
                            System.Convert.ToString(source.Length)
                        }
                    });
                }
            }
            else
            {
                attributes.Add(new CsSchema.Attribute("Column")
                {
                    ArgumentExpressions = new string[]
                    {
                        $"\"{source.Name}\"",
                        $"TypeName = \"{source.TypeFullName}\""
                    }
                });
            }
            property.Attributes = attributes.ToArray();
            #endregion
            #region convert type
            switch (source.TypeName)
            {
                case "bigint":
                    property.TypeName = $"long{(source.IsNullable ? "?" : "")}";
                    break;
                case "binary":
                    property.TypeName = "byte[]";
                    break;
                case "bit":
                    property.TypeName = $"bool{(source.IsNullable ? "?" : "")}";
                    break;
                case "char":
                    property.TypeName = "string";
                    break;
                case "date":
                case "datetime":
                case "datetime2":
                    property.TypeName = $"DateTime{(source.IsNullable ? "?" : "")}";
                    break;
                case "datetimeoffset":
                    property.TypeName = $"DateTimeOffset{(source.IsNullable ? "?" : "")}";
                    break;
                case "decimal":
                    property.TypeName = $"decimal{(source.IsNullable ? "?" : "")}";
                    break;
                case "float":
                    property.TypeName = $"double{(source.IsNullable ? "?" : "")}";
                    break;
                case "image":
                    property.TypeName = "byte[]";
                    break;
                case "int":
                    property.TypeName = $"int{(source.IsNullable ? "?" : "")}";
                    break;
                case "money":
                    property.TypeName = $"decimal{(source.IsNullable ? "?" : "")}";
                    break;
                case "nchar":
                    property.TypeName = "string";
                    break;
                case "ntext":
                    property.TypeName = "string";
                    break;
                case "numeric":
                    property.TypeName = $"decimal{(source.IsNullable ? "?" : "")}";
                    break;
                case "nvarchar":
                    property.TypeName = "string";
                    break;
                case "real":
                    property.TypeName = $"float{(source.IsNullable ? "?" : "")}";
                    break;
                case "smalldatetime":
                    property.TypeName = $"DateTime{(source.IsNullable ? "?" : "")}";
                    break;
                case "smallint":
                    property.TypeName = $"short{(source.IsNullable ? "?" : "")}";
                    break;
                case "smallmoney":
                    property.TypeName = $"decimal{(source.IsNullable ? "?" : "")}";
                    break;
                case "sql_variant":
                    property.TypeName = "object";
                    break;
                case "text":
                    property.TypeName = "string";
                    break;
                case "time":
                    property.TypeName = $"TimeSpan{(source.IsNullable ? "?" : "")}";
                    break;
                case "timestamp":
                    property.TypeName = "byte[]";
                    break;
                case "tinyint":
                    property.TypeName = $"byte{(source.IsNullable ? "?" : "")}";
                    break;
                case "uniqueidentifier":
                    property.TypeName = $"Guid{(source.IsNullable ? "?" : "")}";
                    break;
                case "varbinary":
                    property.TypeName = "byte[]";
                    break;
                case "varchar":
                    property.TypeName = "string";
                    break;
                case "xml":
                    property.TypeName = "string";
                    break;
            }
            #endregion
            return property;
        }
    }

    public class CsClassToTsClassConverter : ITypeConverter<CsSchema.Class, TsSchema.Class>
    {
        private readonly IMapper mapper;
        public CsClassToTsClassConverter(IMapper mapper)
        {
            this.mapper = mapper;
        }
        public TsSchema.Class Convert(CsSchema.Class source, TsSchema.Class destination, ResolutionContext context)
        {
            TsSchema.Class @class = new TsSchema.Class();
            @class.Name = source.Name;
            @class.Properties = mapper.Map<TsSchema.Property[]>(source.Properties);
            return @class;
        }
    }

    public class CsPropertyToTsPropertyConverter : ITypeConverter<CsSchema.Property, TsSchema.Property>
    {
        public TsSchema.Property Convert(CsSchema.Property source, TsSchema.Property destination, ResolutionContext context)
        {
            TsSchema.Property property = new TsSchema.Property();
            #region convert type
            property.Name = source.Name.LowerFirst();
            switch (source.TypeName)
            {
                case "string":
                    property.TypeName = "string";
                    break;
                case "bool":
                    property.TypeName = "boolean";
                    break;
                case "bool?":
                    property.TypeName = "boolean | null";
                    break;
                case "short":
                case "int":
                case "long":
                case "float":
                case "double":
                case "decimal":
                    property.TypeName = "number";
                    break;
                case "short?":
                case "int?":
                case "long?":
                case "float?":
                case "double?":
                case "decimal?":
                    property.TypeName = "number | null";
                    break;
                default:
                    property.TypeName = "any";
                    break;
            }
            #endregion
            return property;
        }
    }
    #endregion
}
