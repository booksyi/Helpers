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
            CreateMap<CsSchemaAccess, IEnumerable<SyntaxToken>>().ConvertUsing<CsAccessToSyntaxTokensConverter>();
            CreateMap<CsSchemaUnit, CompilationUnitSyntax>().ConvertUsing<CsUnitToSyntaxConverter>();
            CreateMap<CsSchemaNamespace, NamespaceDeclarationSyntax>().ConvertUsing<CsNamespaceToSyntaxConverter>();
            CreateMap<CsSchemaClass, ClassDeclarationSyntax>().ConvertUsing<CsClassToSyntaxConverter>();
            CreateMap<CsSchemaProperty, PropertyDeclarationSyntax>().ConvertUsing<CsPropertyToSyntaxConverter>();

            CreateMap<DbSchemaTable, CsSchemaClass>().ConvertUsing<DbTableToCsClassConverter>();
            CreateMap<DbSchemaField, CsSchemaProperty>().ConvertUsing<DbFieldToCsPropertyConverter>();
            CreateMap<CsSchemaClass, TsSchemaClass>().ConvertUsing<CsClassToTsClassConverter>();
            CreateMap<CsSchemaProperty, TsSchemaProperty>().ConvertUsing<CsPropertyToTsPropertyConverter>();
        }
    }

    #region Schema To Syntax Converters
    public class CsAccessToSyntaxTokensConverter : ITypeConverter<CsSchemaAccess, IEnumerable<SyntaxToken>>
    {
        public IEnumerable<SyntaxToken> Convert(CsSchemaAccess source, IEnumerable<SyntaxToken> destination, ResolutionContext context)
        {
            switch (source)
            {
                case CsSchemaAccess.Public:
                    yield return SyntaxFactory.Token(SyntaxKind.PublicKeyword);
                    break;
                case CsSchemaAccess.Protected:
                    yield return SyntaxFactory.Token(SyntaxKind.ProtectedKeyword);
                    break;
                case CsSchemaAccess.Internal:
                    yield return SyntaxFactory.Token(SyntaxKind.InternalKeyword);
                    break;
                case CsSchemaAccess.ProtectedInternal:
                    yield return SyntaxFactory.Token(SyntaxKind.ProtectedKeyword);
                    yield return SyntaxFactory.Token(SyntaxKind.InternalKeyword);
                    break;
                case CsSchemaAccess.Private:
                    yield return SyntaxFactory.Token(SyntaxKind.PrivateKeyword);
                    break;
                case CsSchemaAccess.PrivateProtected:
                    yield return SyntaxFactory.Token(SyntaxKind.PrivateKeyword);
                    yield return SyntaxFactory.Token(SyntaxKind.ProtectedKeyword);
                    break;
            }
        }
    }

    public class CsUnitToSyntaxConverter : ITypeConverter<CsSchemaUnit, CompilationUnitSyntax>
    {
        private readonly IMapper mapper;
        public CsUnitToSyntaxConverter(IMapper mapper)
        {
            this.mapper = mapper;
        }

        public CompilationUnitSyntax Convert(CsSchemaUnit source, CompilationUnitSyntax destination, ResolutionContext context)
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
                    syntax = syntax.AddMembers(mapper.Map<NamespaceDeclarationSyntax>(@namespace));
                }
            }
            return syntax;
        }
    }

    public class CsNamespaceToSyntaxConverter : ITypeConverter<CsSchemaNamespace, NamespaceDeclarationSyntax>
    {
        private readonly IMapper mapper;
        public CsNamespaceToSyntaxConverter(IMapper mapper)
        {
            this.mapper = mapper;
        }

        public NamespaceDeclarationSyntax Convert(CsSchemaNamespace source, NamespaceDeclarationSyntax destination, ResolutionContext context)
        {
            var syntax = SyntaxFactory.NamespaceDeclaration(
                SyntaxFactory.ParseName(source.Namespace)).NormalizeWhitespace();
            if (source.Classes != null)
            {
                foreach (CsSchemaClass @class in source.Classes)
                {
                    syntax = syntax.AddMembers(mapper.Map<ClassDeclarationSyntax>(@class));
                }
            }
            return syntax;
        }
    }

    public class CsClassToSyntaxConverter : ITypeConverter<CsSchemaClass, ClassDeclarationSyntax>
    {
        private readonly IMapper mapper;
        public CsClassToSyntaxConverter(IMapper mapper)
        {
            this.mapper = mapper;
        }

        public ClassDeclarationSyntax Convert(CsSchemaClass source, ClassDeclarationSyntax destination, ResolutionContext context)
        {
            var syntax = SyntaxFactory.ClassDeclaration(source.Name)
                .AddModifiers(mapper.Map<IEnumerable<SyntaxToken>>(source.Access).ToArray())
                .NormalizeWhitespace(); ;
            if (source.Properties != null)
            {
                foreach (CsSchemaProperty property in source.Properties)
                {
                    syntax = syntax.AddMembers(mapper.Map<PropertyDeclarationSyntax>(property));
                }
            }
            return syntax;
        }
    }

    public class CsPropertyToSyntaxConverter : ITypeConverter<CsSchemaProperty, PropertyDeclarationSyntax>
    {
        private readonly IMapper mapper;
        public CsPropertyToSyntaxConverter(IMapper mapper)
        {
            this.mapper = mapper;
        }

        public PropertyDeclarationSyntax Convert(CsSchemaProperty source, PropertyDeclarationSyntax destination, ResolutionContext context)
        {
            return SyntaxFactory.PropertyDeclaration(
                SyntaxFactory.ParseTypeName(source.TypeName), source.Name)
                    .AddModifiers(mapper.Map<IEnumerable<SyntaxToken>>(source.Access).ToArray())
                    .AddAccessorListAccessors(
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));
        }
    }
    #endregion
    #region Schema To Schema Converters
    public class DbTableToCsClassConverter : ITypeConverter<DbSchemaTable, CsSchemaClass>
    {
        private readonly IMapper mapper;
        public DbTableToCsClassConverter(IMapper mapper)
        {
            this.mapper = mapper;
        }

        public CsSchemaClass Convert(DbSchemaTable source, CsSchemaClass destination, ResolutionContext context)
        {
            CsSchemaClass @class = new CsSchemaClass();
            @class.Name = source.Name.UpperFirst();
            @class.Properties = mapper.Map<CsSchemaProperty[]>(source.Fields);
            return @class;
        }
    }

    public class DbFieldToCsPropertyConverter : ITypeConverter<DbSchemaField, CsSchemaProperty>
    {
        public CsSchemaProperty Convert(DbSchemaField source, CsSchemaProperty destination, ResolutionContext context)
        {
            CsSchemaProperty property = new CsSchemaProperty() { Name = source.Name };
            #region set attributes
            List<CsSchemaAttribute> attributes = new List<CsSchemaAttribute>();
            if (source.IsIdentity)
            {
                attributes.Add(new CsSchemaAttribute("Key"));
                attributes.Add(new CsSchemaAttribute("DatabaseGenerated")
                {
                    ConstructorParameters = new Dictionary<string, CsSchemaValue>
                    {
                        { "databaseGeneratedOption", new CsSchemaValue("DatabaseGeneratedOption.Identity", false) }
                    }
                });
            }
            if (source.TypeName.In("char", "nchar", "ntext", "nvarchar", "text", "varchar", "xml"))
            {
                if (source.IsNullable == false)
                {
                    attributes.Add(new CsSchemaAttribute("Required"));
                }
                attributes.Add(new CsSchemaAttribute("Column")
                {
                    ConstructorParameters = new Dictionary<string, CsSchemaValue>
                    {
                        { "name", new CsSchemaValue(source.Name) }
                    }
                });
                if (source.Length > 0)
                {
                    attributes.Add(new CsSchemaAttribute("StringLength")
                    {
                        ConstructorParameters = new Dictionary<string, CsSchemaValue>
                        {
                            { "maximumLength", new CsSchemaValue(source.Length) }
                        }
                    });
                }
            }
            else
            {
                attributes.Add(new CsSchemaAttribute("Column")
                {
                    ConstructorParameters = new Dictionary<string, CsSchemaValue>
                    {
                        { "name", new CsSchemaValue(source.Name) }
                    },
                    Properties = new Dictionary<string, CsSchemaValue>
                    {
                        { "TypeName", new CsSchemaValue(source.TypeFullName) }
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

    public class CsClassToTsClassConverter : ITypeConverter<CsSchemaClass, TsSchemaClass>
    {
        private readonly IMapper mapper;
        public CsClassToTsClassConverter(IMapper mapper)
        {
            this.mapper = mapper;
        }
        public TsSchemaClass Convert(CsSchemaClass source, TsSchemaClass destination, ResolutionContext context)
        {
            TsSchemaClass @class = new TsSchemaClass();
            @class.Name = source.Name;
            @class.Properties = mapper.Map<TsSchemaProperty[]>(source.Properties);
            return @class;
        }
    }

    public class CsPropertyToTsPropertyConverter : ITypeConverter<CsSchemaProperty, TsSchemaProperty>
    {
        public TsSchemaProperty Convert(CsSchemaProperty source, TsSchemaProperty destination, ResolutionContext context)
        {
            TsSchemaProperty property = new TsSchemaProperty();
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
