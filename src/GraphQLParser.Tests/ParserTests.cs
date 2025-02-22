using System.Collections;
using System.Runtime.InteropServices;

namespace GraphQLParser.Tests;

public class ParserTests
{
    [Fact]
    public void GraphQLDocument_Source_ShouldBe_Original_String()
    {
        string text = "scalar JSON";
        var doc = text.Parse();
        (doc.Source == text).ShouldBeTrue();

        // just to demonstrate how TryGetString works
        MemoryMarshal.TryGetString(doc.Source, out var str1, out var start1, out var length1).ShouldBeTrue();
        ReferenceEquals(text, str1).ShouldBeTrue();
        start1.ShouldBe(0);
        length1.ShouldBe(11);

        var text2 = text.AsMemory().Slice(1);
        MemoryMarshal.TryGetString(text2, out var str2, out var start2, out var length2).ShouldBeTrue();
        ReferenceEquals(text, str2).ShouldBeTrue();
        start2.ShouldBe(1);
        length2.ShouldBe(10);

        var text3 = text.AsMemory().Slice(2, 4);
        MemoryMarshal.TryGetString(text3, out var str3, out var start3, out var length3).ShouldBeTrue();
        ReferenceEquals(text, str3).ShouldBeTrue();
        start3.ShouldBe(2);
        length3.ShouldBe(4);
    }

    [Theory]
    [InlineData(IgnoreOptions.None)]
    //[InlineData(IgnoreOptions.Comments)]
    [InlineData(IgnoreOptions.Locations)]
    //[InlineData(IgnoreOptions.All)]
    public void Extra_Comments_Should_Read_Correctly(IgnoreOptions options)
    {
        string query = "ExtraComments".ReadGraphQLFile();

        var document = query.Parse(new ParserOptions { Ignore = options });
        document.Definitions.Count.ShouldBe(1);
        // query
        var def = document.Definitions.First() as GraphQLOperationDefinition;
        def.SelectionSet.Selections.Count.ShouldBe(2);
        // person
        var field = def.SelectionSet.Selections.First() as GraphQLField;
        field.SelectionSet.Selections.Count.ShouldBe(1);
        // name
        var subField = field.SelectionSet.Selections.First() as GraphQLField;
        subField.Comment.ShouldBeNull();
        // test
        field = def.SelectionSet.Selections.Last() as GraphQLField;
        field.SelectionSet.Selections.Count.ShouldBe(1);
        field.Comment.ShouldNotBeNull().Value.ShouldBe("comment2");
        // alt
        subField = field.SelectionSet.Selections.First() as GraphQLField;
        subField.Comment.ShouldBeNull();
        // extra document comments
        document.UnattachedComments.Count.ShouldBe(3);
        document.UnattachedComments[0][0].Value.ShouldBe("comment1");
        document.UnattachedComments[1][0].Value.ShouldBe("comment3");
        document.UnattachedComments[2][0].Value.ShouldBe("comment4");
    }

    [Theory]
    //[InlineData(IgnoreOptions.None)]
    [InlineData(IgnoreOptions.Comments)]
    //[InlineData(IgnoreOptions.Locations)]
    [InlineData(IgnoreOptions.All)]
    public void Comments_Can_Be_Ignored(IgnoreOptions options)
    {
        const string query = @"
{
    #comment
    person
    # comment2
}";

        var document = query.Parse(new ParserOptions { Ignore = options });
        document.UnattachedComments.ShouldBeNull();
        document.Definitions.Count.ShouldBe(1);
        var def = document.Definitions.First() as GraphQLOperationDefinition;
        def.SelectionSet.Selections.Count.ShouldBe(1);
        def.Comment.ShouldBeNull();
        var field = def.SelectionSet.Selections.First() as GraphQLField;
        field.Comment.ShouldBeNull();
    }

    [Theory]
    [InlineData(IgnoreOptions.None)]
    //[InlineData(IgnoreOptions.Comments)]
    [InlineData(IgnoreOptions.Locations)]
    //[InlineData(IgnoreOptions.All)]
    public void Comments_on_FragmentSpread_Should_Read_Correctly(IgnoreOptions options)
    {
        string query = "CommentsOnFragmentSpread".ReadGraphQLFile();

        var document = query.Parse(new ParserOptions { Ignore = options });
        document.Definitions.Count.ShouldBe(2);
        var def = document.Definitions.First() as GraphQLOperationDefinition;
        def.SelectionSet.Selections.Count.ShouldBe(1);
        var field = def.SelectionSet.Selections.First() as GraphQLField;
        field.SelectionSet.Selections.Count.ShouldBe(1);
        var spread = field.SelectionSet.Selections.First() as GraphQLFragmentSpread;
        spread.Comment.ShouldNotBeNull().Value.ShouldBe("comment");
        spread.FragmentName.Comment.Value.ShouldBe("comment on fragment name 1");

        var frag = document.Definitions.Last() as GraphQLFragmentDefinition;
        frag.Comment.ShouldBeNull();
        frag.FragmentName.Comment.Value.ShouldBe("comment on fragment name 2");
    }

    [Theory]
    [InlineData(IgnoreOptions.None)]
    //[InlineData(IgnoreOptions.Comments)]
    [InlineData(IgnoreOptions.Locations)]
    //[InlineData(IgnoreOptions.All)]
    public void Comments_on_Values_Should_Read_Correctly(IgnoreOptions options)
    {
        string query = "CommentsOnValues".ReadGraphQLFile();

        var document = query.Parse(new ParserOptions { Ignore = options });
        document.Definitions.Count.ShouldBe(1);
        var def = document.Definitions.First() as GraphQLOperationDefinition;
        def.SelectionSet.Selections.Count.ShouldBe(1);
        var field = def.SelectionSet.Selections.First() as GraphQLField;
        field.SelectionSet.Selections.Count.ShouldBe(1);
        field.Arguments.Count.ShouldBe(9);

        var boolValue = field.Arguments[0].Value.ShouldBeAssignableTo<GraphQLBooleanValue>();
        boolValue.Comment.ShouldNotBeNull().Value.ShouldBe("comment for bool");

        var nullValue = field.Arguments[1].Value.ShouldBeAssignableTo<GraphQLNullValue>();
        nullValue.Comment.ShouldNotBeNull().Value.ShouldBe("comment for null");

        var enumValue = field.Arguments[2].Value.ShouldBeAssignableTo<GraphQLEnumValue>();
        enumValue.Comment.ShouldNotBeNull().Value.ShouldBe("comment for enum");

        var listValue = field.Arguments[3].Value.ShouldBeAssignableTo<GraphQLListValue>();
        listValue.Comment.ShouldNotBeNull().Value.ShouldBe("comment for list");

        var objValue = field.Arguments[4].Value.ShouldBeAssignableTo<GraphQLObjectValue>();
        objValue.Comment.ShouldNotBeNull().Value.ShouldBe("comment for object");

        var intValue = field.Arguments[5].Value.ShouldBeAssignableTo<GraphQLIntValue>();
        intValue.Comment.ShouldNotBeNull().Value.ShouldBe("comment for int");

        var floatValue = field.Arguments[6].Value.ShouldBeAssignableTo<GraphQLFloatValue>();
        floatValue.Comment.ShouldNotBeNull().Value.ShouldBe("comment for float");

        var stringValue = field.Arguments[7].Value.ShouldBeAssignableTo<GraphQLStringValue>();
        stringValue.Comment.ShouldNotBeNull().Value.ShouldBe("comment for string");

        var varValue = field.Arguments[8].Value.ShouldBeAssignableTo<GraphQLVariable>();
        varValue.Comment.ShouldNotBeNull().Value.ShouldBe("comment for variable");
    }

    [Theory]
    [InlineData(IgnoreOptions.None)]
    //[InlineData(IgnoreOptions.Comments)]
    [InlineData(IgnoreOptions.Locations)]
    //[InlineData(IgnoreOptions.All)]
    public void Comments_on_FragmentInline_Should_Read_Correctly(IgnoreOptions options)
    {
        string query = "CommentsOnInlineFragment".ReadGraphQLFile();

        var document = query.Parse(new ParserOptions { Ignore = options });
        document.Definitions.Count.ShouldBe(1);
        var def = document.Definitions.First() as GraphQLOperationDefinition;
        def.SelectionSet.Selections.Count.ShouldBe(1);
        var field = def.SelectionSet.Selections.First() as GraphQLField;
        field.SelectionSet.Selections.Count.ShouldBe(1);
        var fragment = field.SelectionSet.Selections.First() as GraphQLInlineFragment;
        fragment.Comment.ShouldNotBeNull().Value.ShouldBe("comment");
    }

    [Theory]
    [InlineData(IgnoreOptions.None)]
    //[InlineData(IgnoreOptions.Comments)]
    [InlineData(IgnoreOptions.Locations)]
    //[InlineData(IgnoreOptions.All)]
    public void Comments_on_Arguments_Should_Read_Correctly(IgnoreOptions options)
    {
        string query = "CommentsOnArguments".ReadGraphQLFile();

        var document = query.Parse(new ParserOptions { Ignore = options });
        document.Definitions.Count.ShouldBe(1);
        var def = document.Definitions[0] as GraphQLOperationDefinition;
        def.SelectionSet.Selections.Count.ShouldBe(1);
        var field = def.SelectionSet.Selections.First() as GraphQLField;
        field.Arguments.Count.ShouldBe(2);
        field.Arguments.Comment.ShouldNotBeNull().Value.ShouldBe("arguments comment");
        var obj = field.Arguments[1].Value.ShouldBeAssignableTo<GraphQLObjectValue>();
        obj.Fields.Count.ShouldBe(1);
        obj.Fields[0].Name.Value.ShouldBe("z");
        obj.Fields[0].Comment.Value.ShouldBe("comment on object field");
    }

    [Theory]
    [InlineData(IgnoreOptions.None)]
    //[InlineData(IgnoreOptions.Comments)]
    [InlineData(IgnoreOptions.Locations)]
    //[InlineData(IgnoreOptions.All)]
    public void Comments_on_NamedTypes_Should_Read_Correctly(IgnoreOptions options)
    {
        string query = "CommentsOnNamedType".ReadGraphQLFile();

        var document = query.Parse(new ParserOptions { Ignore = options });
        document.Definitions.Count.ShouldBe(5);

        var def1 = document.Definitions[0] as GraphQLOperationDefinition;
        var field = def1.SelectionSet.Selections[0] as GraphQLField;
        var frag = field.SelectionSet.Selections[0] as GraphQLInlineFragment;
        frag.TypeCondition.Type.Comment.Value.ShouldBe("comment for named type from TypeCondition");

        var def2 = document.Definitions[1] as GraphQLObjectTypeDefinition;
        def2.Interfaces[0].Comment.Value.ShouldBe("comment for named type from ImplementsInterfaces");

        var def3 = document.Definitions[2] as GraphQLSchemaDefinition;
        def3.OperationTypes[0].Type.Comment.Value.ShouldBe("comment for named type from RootOperationTypeDefinition");

        var def4 = document.Definitions[3] as GraphQLObjectTypeDefinition;
        def4.Fields[0].Type.Comment.Value.ShouldBe("comment for named type from Type");

        var def5 = document.Definitions[4] as GraphQLUnionTypeDefinition;
        def5.Types[1].Comment.Value.ShouldBe("comment for named type from UnionMemberTypes");
    }

    [Theory]
    [InlineData(IgnoreOptions.None)]
    //[InlineData(IgnoreOptions.Comments)]
    [InlineData(IgnoreOptions.Locations)]
    //[InlineData(IgnoreOptions.All)]
    public void Comments_on_SelectionSet_Should_Read_Correctly(IgnoreOptions options)
    {
        string query = "CommentsOnSelectionSet".ReadGraphQLFile();

        var document = query.Parse(new ParserOptions { Ignore = options });
        document.Definitions.Count.ShouldBe(1);
        var def = document.Definitions[0] as GraphQLOperationDefinition;
        def.SelectionSet.Comment.Value.ShouldBe("comment on selection set");
    }

    [Theory]
    [InlineData(IgnoreOptions.None)]
    //[InlineData(IgnoreOptions.Comments)]
    [InlineData(IgnoreOptions.Locations)]
    //[InlineData(IgnoreOptions.All)]
    public void Comments_on_RootOperationType_Should_Read_Correctly(IgnoreOptions options)
    {
        string query = "CommentsOnRootOperationType".ReadGraphQLFile();

        var document = query.Parse(new ParserOptions { Ignore = options });
        document.Definitions.Count.ShouldBe(1);
        var def = document.Definitions[0] as GraphQLSchemaDefinition;
        def.OperationTypes[0].Comment.Value.ShouldBe("comment for root operation type");
    }

    [Theory]
    [InlineData(IgnoreOptions.None)]
    //[InlineData(IgnoreOptions.Comments)]
    [InlineData(IgnoreOptions.Locations)]
    //[InlineData(IgnoreOptions.All)]
    public void Comments_on_Directive_Should_Read_Correctly(IgnoreOptions options)
    {
        string query = "CommentsOnDirective".ReadGraphQLFile();

        var document = query.Parse(new ParserOptions { Ignore = options });
        document.Definitions.Count.ShouldBe(1);
        var def = document.Definitions[0] as GraphQLScalarTypeDefinition;
        def.Directives[0].Comment.Value.ShouldBe("comment for directive");
    }

    [Theory]
    [InlineData(IgnoreOptions.None)]
    //[InlineData(IgnoreOptions.Comments)]
    [InlineData(IgnoreOptions.Locations)]
    //[InlineData(IgnoreOptions.All)]
    public void Comments_on_Type_Should_Read_Correctly(IgnoreOptions options)
    {
        string query = "CommentsOnType".ReadGraphQLFile();

        var document = query.Parse(new ParserOptions { Ignore = options });
        document.Definitions.Count.ShouldBe(3);
        var def = document.Definitions[0] as GraphQLObjectTypeDefinition;
        def.Comment.Value.ShouldBe("very good type");
        def.Interfaces.Comment.Value.ShouldBe("comment for implemented interfaces");
        def.Fields.Comment.Value.ShouldBe("comment for fields definition");
        def.Fields[0].Type.Comment.Value.ShouldBe("comment for named type");
        def.Fields[0].Arguments.Comment.Value.ShouldBe("comment for arguments definition");
        def.Fields[1].Type.Comment.Value.ShouldBe("comment for nonnull type");
        def.Fields[2].Type.Comment.Value.ShouldBe("comment for list type");
        (def.Fields[2].Type as GraphQLListType).Type.Comment.Value.ShouldBe("comment for item type");

        var ext1 = document.Definitions[1] as GraphQLObjectTypeExtension;
        ext1.Comment.Value.ShouldBe("forgot about address!");

        var ext2 = document.Definitions[2] as GraphQLInterfaceTypeExtension;
        ext2.Comment.Value.ShouldBe("forgot about vip!");
    }

    [Theory]
    [InlineData(IgnoreOptions.None)]
    //[InlineData(IgnoreOptions.Comments)]
    [InlineData(IgnoreOptions.Locations)]
    //[InlineData(IgnoreOptions.All)]
    public void Comments_on_Input_Should_Read_Correctly(IgnoreOptions options)
    {
        string query = "CommentsOnInput".ReadGraphQLFile();

        var document = query.Parse(new ParserOptions { Ignore = options });
        document.Definitions.Count.ShouldBe(2);
        var def = document.Definitions[0] as GraphQLInputObjectTypeDefinition;
        def.Comment.Value.ShouldBe("very good input");
        def.Fields.Comment.Value.ShouldBe("comment for input fields definition");
        def.Fields[0].Type.Comment.Value.ShouldBe("comment for named type");
        def.Fields[1].Type.Comment.Value.ShouldBe("comment for nonnull type");
        def.Fields[2].Type.Comment.Value.ShouldBe("comment for list type");
        (def.Fields[2].Type as GraphQLListType).Type.Comment.Value.ShouldBe("comment for item type");

        var ext = document.Definitions[1] as GraphQLInputObjectTypeExtension;
        ext.Comment.Value.ShouldBe("forgot about address!");
    }

    [Theory]
    [InlineData(IgnoreOptions.None)]
    //[InlineData(IgnoreOptions.Comments)]
    [InlineData(IgnoreOptions.Locations)]
    //[InlineData(IgnoreOptions.All)]
    public void Comments_on_Alias_Should_Read_Correctly(IgnoreOptions options)
    {
        string query = "CommentsOnAlias".ReadGraphQLFile();

        var document = query.Parse(new ParserOptions { Ignore = options });
        document.Definitions.Count.ShouldBe(1);
        var def = document.Definitions[0] as GraphQLOperationDefinition;
        def.SelectionSet.Selections.Count.ShouldBe(1);
        var field = def.SelectionSet.Selections[0].ShouldBeAssignableTo<GraphQLField>();
        field.Comment.Value.ShouldBe("field comment! not alias!");
        field.Alias.Name.Value.ShouldBe("a");
        field.Alias.Comment.ShouldBeNull();
        field.Name.Value.ShouldBe("name");
        field.Name.Comment.Value.ShouldBe("field name (GraphQLName) comment");

        document.UnattachedComments.Count.ShouldBe(1);
        document.UnattachedComments[0][0].Value.ShouldBe("colon comment");
    }

    [Theory]
    [InlineData(IgnoreOptions.None)]
    //[InlineData(IgnoreOptions.Comments)]
    [InlineData(IgnoreOptions.Locations)]
    //[InlineData(IgnoreOptions.All)]
    public void Comments_on_DirectiveDefinition_Should_Read_Correctly(IgnoreOptions options)
    {
        string query = "CommentsOnDirectiveDefinition".ReadGraphQLFile();

        var document = query.Parse(new ParserOptions { Ignore = options });
        document.Definitions.Count.ShouldBe(1);
        var def = document.Definitions[0] as GraphQLDirectiveDefinition;
        def.Comment.Value.ShouldBe("very good directive");
        def.Locations.Comment.Value.ShouldBe("comment for directive locations");

        document.UnattachedComments.ShouldBeNull();
    }

    [Theory]
    [InlineData(IgnoreOptions.None)]
    //[InlineData(IgnoreOptions.Comments)]
    [InlineData(IgnoreOptions.Locations)]
    //[InlineData(IgnoreOptions.All)]
    public void Comments_on_Enum_Should_Read_Correctly(IgnoreOptions options)
    {
        string query = "CommentsOnEnum".ReadGraphQLFile();

        var document = query.Parse(new ParserOptions { Ignore = options });
        document.Definitions.Count.ShouldBe(2);
        document.OperationWithName("qwerty").ShouldBeNull();
        document.OperationWithName("").ShouldBeNull();
        var def = document.Definitions[0] as GraphQLEnumTypeDefinition;
        def.Comment.Value.ShouldBe("very good colors");
        def.Values.Comment.Value.ShouldBe("values");
        def.Values[0].Comment.Value.ShouldBe("not green");
        def.Values[1].Comment.Value.ShouldBe("not red");

        var ext = document.Definitions[1] as GraphQLEnumTypeExtension;
        ext.Comment.Value.ShouldBe("forgot about orange!");

        document.UnattachedComments.ShouldBeNull();
    }

    [Theory]
    [InlineData(IgnoreOptions.None)]
    //[InlineData(IgnoreOptions.Comments)]
    [InlineData(IgnoreOptions.Locations)]
    //[InlineData(IgnoreOptions.All)]
    public void Comments_on_Schema_Should_Read_Correctly(IgnoreOptions options)
    {
        string query = "CommentsOnSchema".ReadGraphQLFile();

        var document = query.Parse(new ParserOptions { Ignore = options });
        document.Definitions.Count.ShouldBe(2);
        var def = document.Definitions[0] as GraphQLSchemaDefinition;
        def.Comment.Value.ShouldBe("very good schema");

        var ext = document.Definitions[1] as GraphQLSchemaExtension;
        ext.Comment.Value.ShouldBe("forgot about mutation!");

        document.UnattachedComments.ShouldBeNull();
    }

    [Theory]
    [InlineData(IgnoreOptions.None)]
    //[InlineData(IgnoreOptions.Comments)]
    [InlineData(IgnoreOptions.Locations)]
    //[InlineData(IgnoreOptions.All)]
    public void Comments_on_Scalar_Should_Read_Correctly(IgnoreOptions options)
    {
        string query = "CommentsOnScalar".ReadGraphQLFile();

        var document = query.Parse(new ParserOptions { Ignore = options });
        document.Definitions.Count.ShouldBe(2);
        var def = document.Definitions[0] as GraphQLScalarTypeDefinition;
        def.Comment.Value.ShouldBe("very good scalar");

        var ext = document.Definitions[1] as GraphQLScalarTypeExtension;
        ext.Comment.Value.ShouldBe("forgot about external!");

        document.UnattachedComments.ShouldBeNull();
    }

    [Theory]
    [InlineData(IgnoreOptions.None)]
    //[InlineData(IgnoreOptions.Comments)]
    [InlineData(IgnoreOptions.Locations)]
    //[InlineData(IgnoreOptions.All)]
    public void Comments_on_Union_Should_Read_Correctly(IgnoreOptions options)
    {
        string query = "CommentsOnUnion".ReadGraphQLFile();

        var document = query.Parse(new ParserOptions { Ignore = options });
        document.Definitions.Count.ShouldBe(2);
        var def = document.Definitions[0] as GraphQLUnionTypeDefinition;
        def.Comment.Value.ShouldBe("very good union");
        def.Types.Comment.Value.ShouldBe("comment for union members");

        var ext = document.Definitions[1] as GraphQLUnionTypeExtension;
        ext.Comment.Value.ShouldBe("forgot about C!");

        document.UnattachedComments.ShouldBeNull();
    }

    [Theory]
    [InlineData(IgnoreOptions.None)]
    //[InlineData(IgnoreOptions.Comments)]
    [InlineData(IgnoreOptions.Locations)]
    //[InlineData(IgnoreOptions.All)]
    public void Comments_on_Variable_Should_Read_Correctly(IgnoreOptions options)
    {
        string query = "CommentsOnVariables".ReadGraphQLFile();

        var document = query.Parse(new ParserOptions { Ignore = options });
        document.Definitions.Count.ShouldBe(1);
        var def = document.Definitions.First() as GraphQLOperationDefinition;
        def.Variables.Count.ShouldBe(3);
        def.Variables.Comment.Value.ShouldBe("very good variables definition");
        def.Variables[0].Comment.ShouldNotBeNull().Value.ShouldBe("comment1");
        def.Variables.Skip(1).First().Comment.ShouldBeNull();
        def.Variables.Skip(2).First().Comment.ShouldNotBeNull().Value.ShouldBe("comment3");
    }

    [Theory]
    [InlineData(IgnoreOptions.None)]
    //[InlineData(IgnoreOptions.Comments)]
    [InlineData(IgnoreOptions.Locations)]
    //[InlineData(IgnoreOptions.All)]
    public void Comments_On_SelectionSet_Should_Read_Correctly(IgnoreOptions options)
    {
        var document = @"
query {
    # a comment below query
    field1
    field2
    #second comment
    field3
}
".Parse(new ParserOptions { Ignore = options });
        document.Definitions.Count.ShouldBe(1);
        var def = document.Definitions.First() as GraphQLOperationDefinition;
        def.SelectionSet.Selections.Count.ShouldBe(3);
        def.SelectionSet.Selections.First().Comment.ShouldNotBeNull().Value.ShouldBe(" a comment below query");
        def.SelectionSet.Selections.Skip(1).First().Comment.ShouldBe(null);
        def.SelectionSet.Selections.Skip(2).First().Comment.ShouldNotBeNull().Value.ShouldBe("second comment");
    }

    [Theory]
    [InlineData(IgnoreOptions.None)]
    //[InlineData(IgnoreOptions.Comments)]
    [InlineData(IgnoreOptions.Locations)]
    //[InlineData(IgnoreOptions.All)]
    public void Comments_On_Enum_Definitions_Should_Read_Correctly(IgnoreOptions options)
    {
        var document = @"
# different animals
enum Animal {
    #a cat
    Cat
    #a dog
    Dog
    Octopus
    #bird is the word
    Bird
}

input Parameter {
    #any value
    Value: String
}

scalar JSON
".Parse(new ParserOptions { Ignore = options });
        document.Definitions.Count.ShouldBe(3);
        var d1 = document.Definitions.First() as GraphQLEnumTypeDefinition;
        d1.Name.Value.ShouldBe("Animal");
        d1.Comment.ShouldNotBeNull().Value.ShouldBe(" different animals");
        d1.Values[0].Name.Value.ShouldBe("Cat");
        d1.Values[0].Comment.ShouldNotBeNull();
        d1.Values[0].Comment.Value.ShouldBe("a cat");
        d1.Values.Skip(2).First().Name.Value.ShouldBe("Octopus");
        d1.Values.Skip(2).First().Comment.ShouldBeNull();

        var d2 = document.Definitions.Skip(1).First() as GraphQLInputObjectTypeDefinition;
        d2.Name.Value.ShouldBe("Parameter");
        d2.Comment.ShouldBeNull();
        d2.Fields.Count.ShouldBe(1);
        d2.Fields[0].Comment.Value.ShouldBe("any value");
    }

    [Theory]
    [InlineData(IgnoreOptions.None)]
    [InlineData(IgnoreOptions.Comments)]
    //[InlineData(IgnoreOptions.Locations)]
    //[InlineData(IgnoreOptions.All)]
    public void Parse_FieldInput_HasCorrectLocations(IgnoreOptions options)
    {
        // { field }
        var document = ParseGraphQLFieldSource(options);

        document.Location.ShouldBe(new GraphQLLocation(0, 9)); // { field }
        document.Definitions.First().Location.ShouldBe(new GraphQLLocation(0, 9)); // { field }
        (document.Definitions.First() as GraphQLOperationDefinition).SelectionSet.Location.ShouldBe(new GraphQLLocation(0, 9)); // { field }
        (document.Definitions.First() as GraphQLOperationDefinition).SelectionSet.Selections.First().Location.ShouldBe(new GraphQLLocation(2, 7)); // field
    }

    [Theory]
    [InlineData(IgnoreOptions.None)]
    [InlineData(IgnoreOptions.Comments)]
    [InlineData(IgnoreOptions.Locations)]
    [InlineData(IgnoreOptions.All)]
    public void Parse_FieldInput_HasOneOperationDefinition(IgnoreOptions options)
    {
        var document = ParseGraphQLFieldSource(options);

        document.Definitions.First().Kind.ShouldBe(ASTNodeKind.OperationDefinition);
    }

    [Theory]
    [InlineData(IgnoreOptions.None)]
    [InlineData(IgnoreOptions.Comments)]
    [InlineData(IgnoreOptions.Locations)]
    [InlineData(IgnoreOptions.All)]
    public void Parse_FieldInput_NameIsNull(IgnoreOptions options)
    {
        var document = ParseGraphQLFieldSource(options);

        GetSingleOperationDefinition(document).Name.ShouldBeNull();
    }

    [Theory]
    [InlineData(IgnoreOptions.None)]
    [InlineData(IgnoreOptions.Comments)]
    [InlineData(IgnoreOptions.Locations)]
    [InlineData(IgnoreOptions.All)]
    public void Parse_FieldInput_OperationIsQuery(IgnoreOptions options)
    {
        var document = ParseGraphQLFieldSource(options);

        GetSingleOperationDefinition(document).Operation.ShouldBe(OperationType.Query);
    }

    [Theory]
    [InlineData(IgnoreOptions.None)]
    [InlineData(IgnoreOptions.Comments)]
    [InlineData(IgnoreOptions.Locations)]
    [InlineData(IgnoreOptions.All)]
    public void Parse_FieldInput_ReturnsDocumentNode(IgnoreOptions options)
    {
        var document = ParseGraphQLFieldSource(options);

        document.Kind.ShouldBe(ASTNodeKind.Document);
    }

    [Theory]
    [InlineData(IgnoreOptions.None)]
    [InlineData(IgnoreOptions.Comments)]
    [InlineData(IgnoreOptions.Locations)]
    [InlineData(IgnoreOptions.All)]
    public void Parse_FieldInput_SelectionSetContainsSingleField(IgnoreOptions options)
    {
        var document = ParseGraphQLFieldSource(options);

        GetSingleSelection(document).Kind.ShouldBe(ASTNodeKind.Field);
    }

    [Theory]
    [InlineData(IgnoreOptions.None)]
    [InlineData(IgnoreOptions.Comments)]
    //[InlineData(IgnoreOptions.Locations)]
    //[InlineData(IgnoreOptions.All)]
    public void Parse_FieldWithOperationTypeAndNameInput_HasCorrectLocations(IgnoreOptions options)
    {
        // mutation Foo { field }
        var document = ParseGraphQLFieldWithOperationTypeAndNameSource(options);

        document.Location.ShouldBe(new GraphQLLocation(0, 22));
        document.Definitions.First().Location.ShouldBe(new GraphQLLocation(0, 22));
        (document.Definitions.First() as GraphQLOperationDefinition).Name.Location.ShouldBe(new GraphQLLocation(9, 12)); // Foo
        (document.Definitions.First() as GraphQLOperationDefinition).SelectionSet.Location.ShouldBe(new GraphQLLocation(13, 22)); // { field }
        (document.Definitions.First() as GraphQLOperationDefinition).SelectionSet.Selections.First().Location.ShouldBe(new GraphQLLocation(15, 20)); // field
    }

    [Theory]
    [InlineData(IgnoreOptions.None)]
    [InlineData(IgnoreOptions.Comments)]
    [InlineData(IgnoreOptions.Locations)]
    [InlineData(IgnoreOptions.All)]
    public void Parse_FieldWithOperationTypeAndNameInput_HasOneOperationDefinition(IgnoreOptions options)
    {
        var document = ParseGraphQLFieldWithOperationTypeAndNameSource(options);

        document.Definitions.First().Kind.ShouldBe(ASTNodeKind.OperationDefinition);
    }

    [Theory]
    [InlineData(IgnoreOptions.None)]
    [InlineData(IgnoreOptions.Comments)]
    [InlineData(IgnoreOptions.Locations)]
    [InlineData(IgnoreOptions.All)]
    public void Parse_FieldWithOperationTypeAndNameInput_NameIsNull(IgnoreOptions options)
    {
        var document = ParseGraphQLFieldWithOperationTypeAndNameSource(options);

        GetSingleOperationDefinition(document).Name.Value.ShouldBe("Foo");
    }

    [Theory]
    [InlineData(IgnoreOptions.None)]
    [InlineData(IgnoreOptions.Comments)]
    [InlineData(IgnoreOptions.Locations)]
    [InlineData(IgnoreOptions.All)]
    public void Parse_FieldWithOperationTypeAndNameInput_OperationIsQuery(IgnoreOptions options)
    {
        var document = ParseGraphQLFieldWithOperationTypeAndNameSource(options);

        GetSingleOperationDefinition(document).Operation.ShouldBe(OperationType.Mutation);
    }

    [Theory]
    [InlineData(IgnoreOptions.None)]
    [InlineData(IgnoreOptions.Comments)]
    [InlineData(IgnoreOptions.Locations)]
    [InlineData(IgnoreOptions.All)]
    public void Parse_FieldWithOperationTypeAndNameInput_ReturnsDocumentNode(IgnoreOptions options)
    {
        var document = ParseGraphQLFieldWithOperationTypeAndNameSource(options);

        document.Kind.ShouldBe(ASTNodeKind.Document);
    }

    [Theory]
    [InlineData(IgnoreOptions.None)]
    [InlineData(IgnoreOptions.Comments)]
    [InlineData(IgnoreOptions.Locations)]
    [InlineData(IgnoreOptions.All)]
    public void Parse_FieldWithOperationTypeAndNameInput_SelectionSetContainsSingleFieldWithOperationTypeAndNameSelection(IgnoreOptions options)
    {
        var document = ParseGraphQLFieldWithOperationTypeAndNameSource(options);

        GetSingleSelection(document).Kind.ShouldBe(ASTNodeKind.Field);
    }

    [Theory]
    [InlineData(IgnoreOptions.None)]
    [InlineData(IgnoreOptions.Comments)]
    [InlineData(IgnoreOptions.Locations)]
    [InlineData(IgnoreOptions.All)]
    public void Parse_KitchenSink_DoesNotThrowError(IgnoreOptions options)
    {
        var document = "KitchenSink".ReadGraphQLFile().Parse(new ParserOptions { Ignore = options });

        document.OperationsCount().ShouldBe(5);
        document.FragmentsCount().ShouldBe(1);
        document.FindFragmentDefinition("qwerty").ShouldBeNull();
        document.FindFragmentDefinition("frag").ShouldNotBeNull();
        document.OperationWithName("qwerty").ShouldBeNull();
        document.OperationWithName("updateStory").ShouldNotBeNull().Name.Value.ShouldBe("updateStory");
        document.OperationWithName("").ShouldNotBeNull().Name.Value.ShouldBe("queryName");

        var typeDef = document.Definitions.OfType<GraphQLObjectTypeDefinition>().First(d => d.Name.Value == "Foo");
        var fieldDef = typeDef.Fields.First(d => d.Name.Value == "three");
        if (options.HasFlag(IgnoreOptions.Comments))
        {
            fieldDef.Comment.ShouldBeNull();
        }
        else
        {
            fieldDef.Comments.ShouldNotBeNull();
            fieldDef.Comments.Count.ShouldBe(3);
            fieldDef.Comments[0].Value.ShouldBe(" multiline comments");
            fieldDef.Comments[1].Value.ShouldBe(" with very importand description #");
            fieldDef.Comments[2].Value.ShouldBe(" # and symbol # and ##");
        }

        // Schema description
        // https://github.com/graphql/graphql-spec/pull/466
        var comments = document.Definitions.OfType<GraphQLSchemaDefinition>().First().Comments;
        if (options.HasFlag(IgnoreOptions.Comments))
        {
            comments.ShouldBeNull();
        }
        else
        {
            comments.ShouldNotBeNull();
            (comments[0].Value == " Copyright (c) 2015, Facebook, Inc.").ShouldBeTrue();
        }
    }

    [Theory]
    [InlineData(IgnoreOptions.None)]
    [InlineData(IgnoreOptions.Comments)]
    [InlineData(IgnoreOptions.Locations)]
    [InlineData(IgnoreOptions.All)]
    public void Parse_NullInput_EmptyDocument(IgnoreOptions options)
    {
        var document = ((string)null).Parse(new ParserOptions { Ignore = options });

        document.Definitions.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(IgnoreOptions.None)]
    [InlineData(IgnoreOptions.Comments)]
    [InlineData(IgnoreOptions.Locations)]
    [InlineData(IgnoreOptions.All)]
    public void Parse_VariableInlineValues_DoesNotThrowError(IgnoreOptions options)
    {
        "{ field(complex: { a: { b: [ $var ] } }) }".Parse(new ParserOptions { Ignore = options });
    }

    [Theory]
    [InlineData(IgnoreOptions.None)]
    [InlineData(IgnoreOptions.Comments)]
    [InlineData(IgnoreOptions.Locations)]
    [InlineData(IgnoreOptions.All)]
    public void Should_Read_Directives_on_VariableDefinition(IgnoreOptions options)
    {
        var document = "query A($id: String @a @b(priority: 1, managed: true)) { name }".Parse(new ParserOptions { Ignore = options });

        document.Definitions.Count.ShouldBe(1);
        var def = document.Definitions[0].ShouldBeAssignableTo<GraphQLOperationDefinition>();
        def.Variables.Count.ShouldBe(1);
        def.Variables[0].Directives.Count.ShouldBe(2);
        def.Variables[0].Directives[0].Name.Value.ShouldBe("a");
        def.Variables[0].Directives[1].Name.Value.ShouldBe("b");
        def.Variables[0].Directives[1].Arguments.Count.ShouldBe(2);

        // ASTListNode small test
        def.Variables.GetEnumerator().ShouldBe(((IEnumerable)def.Variables).GetEnumerator());
    }

    [Theory]
    [InlineData(IgnoreOptions.None)]
    [InlineData(IgnoreOptions.Comments)]
    [InlineData(IgnoreOptions.Locations)]
    [InlineData(IgnoreOptions.All)]
    public void Should_Read_Directives_on_OperationDefinition(IgnoreOptions options)
    {
        var document = "query A @easy { name }".Parse(new ParserOptions { Ignore = options });

        document.Definitions.Count.ShouldBe(1);
        var def = document.Definitions[0].ShouldBeAssignableTo<GraphQLOperationDefinition>();
        def.Directives.Count.ShouldBe(1);
        def.Directives[0].Name.Value.ShouldBe("easy");
    }

    [Theory]
    [InlineData(IgnoreOptions.None)]
    [InlineData(IgnoreOptions.Comments)]
    [InlineData(IgnoreOptions.Locations)]
    [InlineData(IgnoreOptions.All)]
    public void Should_Read_Directives_on_FragmentSpread(IgnoreOptions options)
    {
        var document = "query { ...spread1 @skip(if: false) }".Parse(new ParserOptions { Ignore = options });

        document.Definitions.Count.ShouldBe(1);
        var def = document.Definitions[0].ShouldBeAssignableTo<GraphQLOperationDefinition>();
        var spread = def.SelectionSet.Selections[0].ShouldBeAssignableTo<GraphQLFragmentSpread>();
        spread.Directives.Count.ShouldBe(1);
        spread.Directives[0].Name.Value.ShouldBe("skip");
    }

    [Theory]
    [InlineData(IgnoreOptions.None)]
    [InlineData(IgnoreOptions.Comments)]
    [InlineData(IgnoreOptions.Locations)]
    [InlineData(IgnoreOptions.All)]
    public void Should_Read_Directives_on_FragmentDefinition(IgnoreOptions options)
    {
        var document = "fragment f on User @documented { name }".Parse(new ParserOptions { Ignore = options });

        document.Definitions.Count.ShouldBe(1);
        var def = document.Definitions[0].ShouldBeAssignableTo<GraphQLFragmentDefinition>();
        def.Directives.Count.ShouldBe(1);
        def.Directives[0].Name.Value.ShouldBe("documented");
    }

    private static GraphQLOperationDefinition GetSingleOperationDefinition(GraphQLDocument document)
    {
        return (GraphQLOperationDefinition)document.Definitions.Single();
    }

    private static ASTNode GetSingleSelection(GraphQLDocument document)
    {
        return GetSingleOperationDefinition(document).SelectionSet.Selections.Single();
    }

    private static GraphQLDocument ParseGraphQLFieldSource(IgnoreOptions options) => "{ field }".Parse(new ParserOptions { Ignore = options });

    private static GraphQLDocument ParseGraphQLFieldWithOperationTypeAndNameSource(IgnoreOptions options) => "mutation Foo { field }".Parse(new ParserOptions { Ignore = options });

    [Theory]
    [InlineData("directive @dir repeatable on FIELD_DEFINITION", true)]
    [InlineData("directive @dir(a: Int) repeatable on FIELD_DEFINITION", true)]
    [InlineData("directive @dir on FIELD_DEFINITION | ENUM_VALUE", false)]
    [InlineData("directive @dir on | FIELD_DEFINITION | ENUM_VALUE", false)]
    [InlineData(@"directive @dir on
FIELD_DEFINITION | ENUM_VALUE", false)]
    [InlineData(@"directive @dir on
FIELD_DEFINITION
| ENUM_VALUE", false)]
    [InlineData(@"directive @dir on
| FIELD_DEFINITION
| ENUM_VALUE", false)]
    [InlineData(@"directive @dir on
|  FIELD_DEFINITION
|          ENUM_VALUE", false)]
    public void Should_Parse_Directives(string text, bool repeatable)
    {
        var document = text.Parse();
        document.ShouldNotBeNull();
        document.Definitions.Count.ShouldBe(1);
        document.Definitions[0].ShouldBeAssignableTo<GraphQLDirectiveDefinition>().Repeatable.ShouldBe(repeatable);
    }

    // http://spec.graphql.org/October2021/#sec--specifiedBy
    [Fact]
    public void Should_Parse_SpecifiedBy()
    {
        string text = @"scalar UUID @specifiedBy(url: ""https://tools.ietf.org/html/rfc4122"")";
        var document = text.Parse();
        document.ShouldNotBeNull();
        document.Definitions.Count.ShouldBe(1);
        var def = document.Definitions[0].ShouldBeAssignableTo<GraphQLScalarTypeDefinition>();
        def.Directives.Count.ShouldBe(1);
        def.Directives[0].Name.Value.ShouldBe("specifiedBy");
        def.Directives[0].Arguments.Count.ShouldBe(1);
        def.Directives[0].Arguments[0].Name.Value.ShouldBe("url");
        var value = def.Directives[0].Arguments[0].Value.ShouldBeAssignableTo<GraphQLStringValue>();
        value.Value.ShouldBe("https://tools.ietf.org/html/rfc4122");
    }

    [Theory]
    [InlineData(IgnoreOptions.None)]
    [InlineData(IgnoreOptions.Comments)]
    [InlineData(IgnoreOptions.Locations)]
    [InlineData(IgnoreOptions.All)]
    public void Should_Parse_Interfaces_Implemented_By_Interface(IgnoreOptions options)
    {
        string text = "InterfacesOnInterface".ReadGraphQLFile();

        var document = text.Parse(new ParserOptions { Ignore = options });
        document.Definitions.Count.ShouldBe(4);
        var def1 = document.Definitions[0].ShouldBeAssignableTo<GraphQLInterfaceTypeDefinition>();
        def1.Name.Value.ShouldBe("Dog");
        def1.Interfaces.ShouldBeNull();

        var def2 = document.Definitions[1].ShouldBeAssignableTo<GraphQLInterfaceTypeDefinition>();
        def2.Name.Value.ShouldBe("Dog");
        def2.Interfaces.Count.ShouldBe(1);
        def2.Interfaces[0].Name.Value.ShouldBe("Eat");

        var def3 = document.Definitions[2].ShouldBeAssignableTo<GraphQLInterfaceTypeDefinition>();
        def3.Name.Value.ShouldBe("Dog");
        def3.Interfaces.Count.ShouldBe(2);
        def3.Interfaces[0].Name.Value.ShouldBe("Eat");
        def3.Interfaces[1].Name.Value.ShouldBe("Sleep");

        var def4 = document.Definitions[3].ShouldBeAssignableTo<GraphQLInterfaceTypeDefinition>();
        def4.Name.Value.ShouldBe("Dog");
        def4.Interfaces.Count.ShouldBe(2);
        def4.Interfaces[0].Name.Value.ShouldBe("Eat");
        def4.Interfaces[1].Name.Value.ShouldBe("Sleep");
        def4.Fields.Count.ShouldBe(1);
        def4.Fields[0].Name.Value.ShouldBe("name");
    }

    [Theory]
    [InlineData("union Animal = Cat | Dog")]
    [InlineData("union Animal = | Cat | Dog")]
    [InlineData(@"union Animal =
Cat | Dog")]
    [InlineData(@"union Animal =
Cat
| Dog")]
    [InlineData(@"union Animal =
| Cat
| Dog")]
    [InlineData(@"union Animal =   
|  Cat
|       Dog")]
    public void Should_Parse_Unions(string text)
    {
        var document = text.Parse();
        document.ShouldNotBeNull();
    }

    [Theory]
    [InlineData("extend scalar Foo @exportable", ASTNodeKind.ScalarTypeExtension)]
    [InlineData("extend type Foo implements Bar @exportable { a: String }", ASTNodeKind.ObjectTypeExtension)]
    [InlineData("extend type Foo implements Bar @exportable", ASTNodeKind.ObjectTypeExtension)]
    [InlineData("extend type Foo implements Bar { a: String }", ASTNodeKind.ObjectTypeExtension)]
    [InlineData("extend type Foo implements Bar", ASTNodeKind.ObjectTypeExtension)]
    [InlineData("extend type Foo { a: String }", ASTNodeKind.ObjectTypeExtension)]
    [InlineData("extend interface Foo implements Bar @exportable { a: String }", ASTNodeKind.InterfaceTypeExtension)]
    [InlineData("extend interface Foo implements Bar @exportable", ASTNodeKind.InterfaceTypeExtension)]
    [InlineData("extend interface Foo implements Bar { a: String }", ASTNodeKind.InterfaceTypeExtension)]
    [InlineData("extend interface Foo { a: String }", ASTNodeKind.InterfaceTypeExtension)]
    [InlineData("extend interface Foo implements Bar", ASTNodeKind.InterfaceTypeExtension)]
    [InlineData("extend union Foo @exportable = A | B", ASTNodeKind.UnionTypeExtension)]
    [InlineData("extend union Foo = A | B", ASTNodeKind.UnionTypeExtension)]
    [InlineData("extend union Foo @exportable", ASTNodeKind.UnionTypeExtension)]
    [InlineData("extend enum Foo @exportable { ONE TWO }", ASTNodeKind.EnumTypeExtension)]
    [InlineData("extend enum Foo { ONE TWO }", ASTNodeKind.EnumTypeExtension)]
    [InlineData("extend enum Foo @exportable", ASTNodeKind.EnumTypeExtension)]
    [InlineData("extend input Foo @exportable { a: String }", ASTNodeKind.InputObjectTypeExtension)]
    [InlineData("extend input Foo { a: String }", ASTNodeKind.InputObjectTypeExtension)]
    [InlineData("extend input Foo @exportable", ASTNodeKind.InputObjectTypeExtension)]
    public void Should_Parse_Extensions(string text, ASTNodeKind kind)
    {
        var document = text.Parse();
        document.ShouldNotBeNull();
        document.Definitions[0].Kind.ShouldBe(kind);
    }

    [Theory]
    [InlineData("scalar Empty", ASTNodeKind.ScalarTypeDefinition)]
    [InlineData("union Empty", ASTNodeKind.UnionTypeDefinition)]
    [InlineData("type Empty", ASTNodeKind.ObjectTypeDefinition)]
    [InlineData("input Empty", ASTNodeKind.InputObjectTypeDefinition)]
    [InlineData("interface Empty", ASTNodeKind.InterfaceTypeDefinition)]
    [InlineData("enum Empty", ASTNodeKind.EnumTypeDefinition)]
    public void Should_Parse_Empty_Types(string text, ASTNodeKind kind)
    {
        var document = text.Parse();
        document.ShouldNotBeNull();
        document.Definitions[0].Kind.ShouldBe(kind);
    }

    [Theory]
    [InlineData(IgnoreOptions.None)]
    [InlineData(IgnoreOptions.Comments)]
    [InlineData(IgnoreOptions.Locations)]
    [InlineData(IgnoreOptions.All)]
    public void Descriptions_Should_Read_Correctly(IgnoreOptions options)
    {
        var document = @"
""Super schema""
schema {
  query: String
}

""A JSON scalar""
scalar JSON

""""""
Human type
""""""
type Human {
  """"""
  Name of human
  """"""
  name: String

  ""Test""
  test(
    ""desc""
    arg: Int
  ): Int
}

""Test interface""
interface TestInterface {
  ""Object name""
  name: String
}

""""""
Test union
""""""
union TestUnion = Test1 | Test2

""Example enum""
enum Colors {
  ""Red"" RED
  ""Blue"" BLUE
}

""""""
This is an example input object
Line two of the description
""""""
input TestInputObject {
    """"""
    The value of the input object
      (any JSON value is accepted)
    """"""
    Value: JSON
}

""Test directive""
directive @TestDirective (
  ""Example""
  Value: Int
) on QUERY
".Parse(new ParserOptions { Ignore = options });
        var defs = document.Definitions;
        defs.Count.ShouldBe(8);

        var schemaDef = defs.Single(x => x is GraphQLSchemaDefinition) as GraphQLSchemaDefinition;
        schemaDef.Description.Value.ShouldBe("Super schema");

        var scalarDef = defs.Single(x => x is GraphQLScalarTypeDefinition) as GraphQLScalarTypeDefinition;
        scalarDef.Name.Value.ShouldBe("JSON");
        scalarDef.Description.Value.ShouldBe("A JSON scalar");

        var objectDef = defs.Single(x => x is GraphQLObjectTypeDefinition) as GraphQLObjectTypeDefinition;
        objectDef.Name.Value.ShouldBe("Human");
        objectDef.Description.Value.ShouldBe("Human type");
        objectDef.Fields.Count.ShouldBe(2);
        objectDef.Fields[0].Name.Value.ShouldBe("name");
        objectDef.Fields[0].Type.ShouldBeAssignableTo<GraphQLNamedType>().Name.Value.ShouldBe("String");
        objectDef.Fields[0].Description.Value.ShouldBe("Name of human");
        objectDef.Fields[1].Name.Value.ShouldBe("test");
        objectDef.Fields[1].Type.ShouldBeAssignableTo<GraphQLNamedType>().Name.Value.ShouldBe("Int");
        objectDef.Fields[1].Description.Value.ShouldBe("Test");
        objectDef.Fields[1].Arguments.Count.ShouldBe(1);
        objectDef.Fields[1].Arguments[0].Name.Value.ShouldBe("arg");
        objectDef.Fields[1].Arguments[0].Description.Value.ShouldBe("desc");
        objectDef.Fields[1].Arguments[0].Type.ShouldBeAssignableTo<GraphQLNamedType>().Name.Value.ShouldBe("Int");

        var interfaceDef = defs.Single(x => x is GraphQLInterfaceTypeDefinition) as GraphQLInterfaceTypeDefinition;
        interfaceDef.Name.Value.ShouldBe("TestInterface");
        interfaceDef.Description.Value.ShouldBe("Test interface");
        interfaceDef.Fields.Count.ShouldBe(1);
        interfaceDef.Fields[0].Name.Value.ShouldBe("name");
        interfaceDef.Fields[0].Type.ShouldBeAssignableTo<GraphQLNamedType>().Name.Value.ShouldBe("String");
        interfaceDef.Fields[0].Description.Value.ShouldBe("Object name");

        var unionDef = defs.Single(x => x is GraphQLUnionTypeDefinition) as GraphQLUnionTypeDefinition;
        unionDef.Name.Value.ShouldBe("TestUnion");
        unionDef.Description.Value.ShouldBe("Test union");
        unionDef.Types.Count.ShouldBe(2);
        unionDef.Types[0].Name.Value.ShouldBe("Test1");
        unionDef.Types[1].Name.Value.ShouldBe("Test2");

        var enumDef = defs.Single(x => x is GraphQLEnumTypeDefinition) as GraphQLEnumTypeDefinition;
        enumDef.Name.Value.ShouldBe("Colors");
        enumDef.Description.Value.ShouldBe("Example enum");
        enumDef.Values.Count.ShouldBe(2);
        enumDef.Values[0].Name.Value.ShouldBe("RED");
        enumDef.Values[0].Description.Value.ShouldBe("Red");
        enumDef.Values[1].Name.Value.ShouldBe("BLUE");
        enumDef.Values[1].Description.Value.ShouldBe("Blue");

        var inputDef = defs.Single(x => x is GraphQLInputObjectTypeDefinition) as GraphQLInputObjectTypeDefinition;
        inputDef.Name.Value.ShouldBe("TestInputObject");
        inputDef.Description.Value.ShouldBe("This is an example input object\nLine two of the description");
        inputDef.Fields.Count.ShouldBe(1);
        inputDef.Fields[0].Name.Value.ShouldBe("Value");
        inputDef.Fields[0].Type.ShouldBeAssignableTo<GraphQLNamedType>().Name.Value.ShouldBe("JSON");
        inputDef.Fields[0].Description.Value.ShouldBe("The value of the input object\n  (any JSON value is accepted)");

        var directiveDef = defs.Single(x => x is GraphQLDirectiveDefinition) as GraphQLDirectiveDefinition;
        directiveDef.Name.Value.ShouldBe("TestDirective");
        directiveDef.Description.Value.ShouldBe("Test directive");
        directiveDef.Arguments.Count.ShouldBe(1);
        directiveDef.Arguments[0].Name.Value.ShouldBe("Value");
        directiveDef.Arguments[0].Type.ShouldBeAssignableTo<GraphQLNamedType>().Name.Value.ShouldBe("Int");
        directiveDef.Arguments[0].Description.Value.ShouldBe("Example");
    }

    [Theory]
    [InlineData(IgnoreOptions.None)]
    [InlineData(IgnoreOptions.Comments)]
    [InlineData(IgnoreOptions.Locations)]
    [InlineData(IgnoreOptions.All)]
    public void Descriptions_WithComments_Should_Read_Correctly_1(IgnoreOptions options)
    {
        var document = @"
# comment -1
""Super schema""
# comment 0
schema {
  query: String
}

# comment 1
""A JSON scalar""
# comment 2
scalar JSON

# comment 3
""""""
Human type
""""""
# comment 4
type Human {
  # comment 5
  """"""
  Name of human
  """"""
  # comment 6
  name: String

  # comment 7
  ""Test""
  # comment 8
  test(
    # comment 9
    ""desc""
    # comment 10
    arg: Int
  ): Int
}

# comment 11
""Test interface""
# comment 12
interface TestInterface {
  # comment 13
  ""Object name""
  # comment 14
  name: String
}

# comment 15
""""""
Test union
""""""
# comment 16
union TestUnion = Test1 | Test2

# comment 17
""Example enum""
# comment 18
enum Colors {
  # comment 19
  ""Red""
  # comment 20
  RED
  # comment 21
  ""Blue""
  # comment 22
  BLUE
}

# comment 23
""""""
This is an example input object
Line two of the description
""""""
# comment 24
input TestInputObject {
    # comment 25
    """"""
    The value of the input object
      (any JSON value is accepted)
    """"""
    # comment 26
    Value: JSON
}

# comment 27
""Test directive""
# comment 28
directive @TestDirective (
  # comment 29
  ""Example""
  # comment 30
  Value: Int
) on QUERY
".Parse(new ParserOptions { Ignore = options });
        var defs = document.Definitions;
        defs.Count.ShouldBe(8);
        var parseComments = !options.HasFlag(IgnoreOptions.Comments);

        var schemaDef = defs.Single(x => x is GraphQLSchemaDefinition) as GraphQLSchemaDefinition;
        schemaDef.Description.Value.ShouldBe("Super schema");
        if (parseComments)
            schemaDef.Comment.Value.ShouldBe(" comment 0");

        var scalarDef = defs.Single(x => x is GraphQLScalarTypeDefinition) as GraphQLScalarTypeDefinition;
        scalarDef.Name.Value.ShouldBe("JSON");
        scalarDef.Description.Value.ShouldBe("A JSON scalar");
        if (parseComments)
            scalarDef.Comment.Value.ShouldBe(" comment 2");

        var objectDef = defs.Single(x => x is GraphQLObjectTypeDefinition) as GraphQLObjectTypeDefinition;
        objectDef.Name.Value.ShouldBe("Human");
        objectDef.Description.Value.ShouldBe("Human type");
        if (parseComments)
            objectDef.Comment.Value.ShouldBe(" comment 4");
        objectDef.Fields.Count.ShouldBe(2);
        objectDef.Fields[0].Name.Value.ShouldBe("name");
        objectDef.Fields[0].Type.ShouldBeAssignableTo<GraphQLNamedType>().Name.Value.ShouldBe("String");
        objectDef.Fields[0].Description.Value.ShouldBe("Name of human");
        if (parseComments)
            objectDef.Fields[0].Comment.Value.ShouldBe(" comment 6");
        objectDef.Fields[1].Name.Value.ShouldBe("test");
        objectDef.Fields[1].Type.ShouldBeAssignableTo<GraphQLNamedType>().Name.Value.ShouldBe("Int");
        objectDef.Fields[1].Description.Value.ShouldBe("Test");
        if (parseComments)
            objectDef.Fields[1].Comment.Value.ShouldBe(" comment 8");
        objectDef.Fields[1].Arguments.Count.ShouldBe(1);
        objectDef.Fields[1].Arguments[0].Name.Value.ShouldBe("arg");
        objectDef.Fields[1].Arguments[0].Description.Value.ShouldBe("desc");
        objectDef.Fields[1].Arguments[0].Type.ShouldBeAssignableTo<GraphQLNamedType>().Name.Value.ShouldBe("Int");
        if (parseComments)
            objectDef.Fields[1].Arguments[0].Comment.Value.ShouldBe(" comment 10");

        var interfaceDef = defs.Single(x => x is GraphQLInterfaceTypeDefinition) as GraphQLInterfaceTypeDefinition;
        interfaceDef.Name.Value.ShouldBe("TestInterface");
        interfaceDef.Description.Value.ShouldBe("Test interface");
        if (parseComments)
            interfaceDef.Comment.Value.ShouldBe(" comment 12");
        interfaceDef.Fields.Count.ShouldBe(1);
        interfaceDef.Fields[0].Name.Value.ShouldBe("name");
        interfaceDef.Fields[0].Type.ShouldBeAssignableTo<GraphQLNamedType>().Name.Value.ShouldBe("String");
        interfaceDef.Fields[0].Description.Value.ShouldBe("Object name");
        if (parseComments)
            interfaceDef.Fields[0].Comment.Value.ShouldBe(" comment 14");

        var unionDef = defs.Single(x => x is GraphQLUnionTypeDefinition) as GraphQLUnionTypeDefinition;
        unionDef.Name.Value.ShouldBe("TestUnion");
        unionDef.Description.Value.ShouldBe("Test union");
        if (parseComments)
            unionDef.Comment.Value.ShouldBe(" comment 16");
        unionDef.Types.Count.ShouldBe(2);
        unionDef.Types[0].Name.Value.ShouldBe("Test1");
        unionDef.Types[1].Name.Value.ShouldBe("Test2");

        var enumDef = defs.Single(x => x is GraphQLEnumTypeDefinition) as GraphQLEnumTypeDefinition;
        enumDef.Name.Value.ShouldBe("Colors");
        enumDef.Description.Value.ShouldBe("Example enum");
        if (parseComments)
            enumDef.Comment.Value.ShouldBe(" comment 18");
        enumDef.Values.Count.ShouldBe(2);
        enumDef.Values[0].Name.Value.ShouldBe("RED");
        enumDef.Values[0].Description.Value.ShouldBe("Red");
        if (parseComments)
            enumDef.Values[0].Comment.Value.ShouldBe(" comment 20");
        enumDef.Values[1].Name.Value.ShouldBe("BLUE");
        enumDef.Values[1].Description.Value.ShouldBe("Blue");
        if (parseComments)
            enumDef.Values[1].Comment.Value.ShouldBe(" comment 22");

        var inputDef = defs.Single(x => x is GraphQLInputObjectTypeDefinition) as GraphQLInputObjectTypeDefinition;
        inputDef.Name.Value.ShouldBe("TestInputObject");
        inputDef.Description.Value.ShouldBe("This is an example input object\nLine two of the description");
        if (parseComments)
            inputDef.Comment.Value.ShouldBe(" comment 24");
        inputDef.Fields.Count.ShouldBe(1);
        inputDef.Fields[0].Name.Value.ShouldBe("Value");
        inputDef.Fields[0].Type.ShouldBeAssignableTo<GraphQLNamedType>().Name.Value.ShouldBe("JSON");
        inputDef.Fields[0].Description.Value.ShouldBe("The value of the input object\n  (any JSON value is accepted)");
        if (parseComments)
            inputDef.Fields[0].Comment.Value.ShouldBe(" comment 26");

        var directiveDef = defs.Single(x => x is GraphQLDirectiveDefinition) as GraphQLDirectiveDefinition;
        directiveDef.Name.Value.ShouldBe("TestDirective");
        directiveDef.Description.Value.ShouldBe("Test directive");
        if (parseComments)
            directiveDef.Comment.Value.ShouldBe(" comment 28");
        directiveDef.Arguments.Count.ShouldBe(1);
        directiveDef.Arguments[0].Name.Value.ShouldBe("Value");
        directiveDef.Arguments[0].Type.ShouldBeAssignableTo<GraphQLNamedType>().Name.Value.ShouldBe("Int");
        directiveDef.Arguments[0].Description.Value.ShouldBe("Example");
        if (parseComments)
            directiveDef.Arguments[0].Comment.Value.ShouldBe(" comment 30");
    }

    [Theory]
    [InlineData(IgnoreOptions.None)]
    [InlineData(IgnoreOptions.Comments)]
    [InlineData(IgnoreOptions.Locations)]
    [InlineData(IgnoreOptions.All)]
    public void Descriptions_WithComments_Should_Read_Correctly_2(IgnoreOptions options)
    {
        var document = @"
""Super schema""
# comment 1
schema {
  query: String
}

""A JSON scalar""
# comment 2
scalar JSON

""""""
Human type
""""""
# comment 4
type Human {
  """"""
  Name of human
  """"""
  # comment 6
  name: String

  ""Test""
  # comment 8
  test(
    ""desc""
    # comment 10
    arg: Int
  ): Int
}

""Test interface""
# comment 12
interface TestInterface {
  ""Object name""
  # comment 14
  name: String
}

""""""
Test union
""""""
# comment 16
union TestUnion = Test1 | Test2

""Example enum""
# comment 18
enum Colors {
  ""Red""
  # comment 20
  RED
  ""Blue""
  # comment 22
  BLUE
}

""""""
This is an example input object
Line two of the description
""""""
# comment 24
input TestInputObject {
    """"""
    The value of the input object
      (any JSON value is accepted)
    """"""
    # comment 26
    Value: JSON
}

""Test directive""
# comment 28
directive @TestDirective (
  ""Example""
  # comment 30
  Value: Int
) on QUERY
".Parse(new ParserOptions { Ignore = options });
        var defs = document.Definitions;
        defs.Count.ShouldBe(8);
        var parseComments = !options.HasFlag(IgnoreOptions.Comments);

        var schemaDef = defs.Single(x => x is GraphQLSchemaDefinition) as GraphQLSchemaDefinition;
        schemaDef.Description.Value.ShouldBe("Super schema");
        if (parseComments)
            schemaDef.Comment.Value.ShouldBe(" comment 1");

        var scalarDef = defs.Single(x => x is GraphQLScalarTypeDefinition) as GraphQLScalarTypeDefinition;
        scalarDef.Name.Value.ShouldBe("JSON");
        scalarDef.Description.Value.ShouldBe("A JSON scalar");
        if (parseComments)
            scalarDef.Comment.Value.ShouldBe(" comment 2");

        var objectDef = defs.Single(x => x is GraphQLObjectTypeDefinition) as GraphQLObjectTypeDefinition;
        objectDef.Name.Value.ShouldBe("Human");
        objectDef.Description.Value.ShouldBe("Human type");
        if (parseComments)
            objectDef.Comment.Value.ShouldBe(" comment 4");
        objectDef.Fields.Count.ShouldBe(2);
        objectDef.Fields[0].Name.Value.ShouldBe("name");
        objectDef.Fields[0].Type.ShouldBeAssignableTo<GraphQLNamedType>().Name.Value.ShouldBe("String");
        objectDef.Fields[0].Description.Value.ShouldBe("Name of human");
        if (parseComments)
            objectDef.Fields[0].Comment.Value.ShouldBe(" comment 6");
        objectDef.Fields[1].Name.Value.ShouldBe("test");
        objectDef.Fields[1].Type.ShouldBeAssignableTo<GraphQLNamedType>().Name.Value.ShouldBe("Int");
        objectDef.Fields[1].Description.Value.ShouldBe("Test");
        if (parseComments)
            objectDef.Fields[1].Comment.Value.ShouldBe(" comment 8");
        objectDef.Fields[1].Arguments.Count.ShouldBe(1);
        objectDef.Fields[1].Arguments[0].Name.Value.ShouldBe("arg");
        objectDef.Fields[1].Arguments[0].Description.Value.ShouldBe("desc");
        objectDef.Fields[1].Arguments[0].Type.ShouldBeAssignableTo<GraphQLNamedType>().Name.Value.ShouldBe("Int");
        if (parseComments)
            objectDef.Fields[1].Arguments[0].Comment.Value.ShouldBe(" comment 10");

        var interfaceDef = defs.Single(x => x is GraphQLInterfaceTypeDefinition) as GraphQLInterfaceTypeDefinition;
        interfaceDef.Name.Value.ShouldBe("TestInterface");
        interfaceDef.Description.Value.ShouldBe("Test interface");
        if (parseComments)
            interfaceDef.Comment.Value.ShouldBe(" comment 12");
        interfaceDef.Fields.Count.ShouldBe(1);
        interfaceDef.Fields[0].Name.Value.ShouldBe("name");
        interfaceDef.Fields[0].Type.ShouldBeAssignableTo<GraphQLNamedType>().Name.Value.ShouldBe("String");
        interfaceDef.Fields[0].Description.Value.ShouldBe("Object name");
        if (parseComments)
            interfaceDef.Fields[0].Comment.Value.ShouldBe(" comment 14");

        var unionDef = defs.Single(x => x is GraphQLUnionTypeDefinition) as GraphQLUnionTypeDefinition;
        unionDef.Name.Value.ShouldBe("TestUnion");
        unionDef.Description.Value.ShouldBe("Test union");
        if (parseComments)
            unionDef.Comment.Value.ShouldBe(" comment 16");
        unionDef.Types.Count.ShouldBe(2);
        unionDef.Types[0].Name.Value.ShouldBe("Test1");
        unionDef.Types[1].Name.Value.ShouldBe("Test2");

        var enumDef = defs.Single(x => x is GraphQLEnumTypeDefinition) as GraphQLEnumTypeDefinition;
        enumDef.Name.Value.ShouldBe("Colors");
        enumDef.Description.Value.ShouldBe("Example enum");
        if (parseComments)
            enumDef.Comment.Value.ShouldBe(" comment 18");
        enumDef.Values.Count.ShouldBe(2);
        enumDef.Values[0].Name.Value.ShouldBe("RED");
        enumDef.Values[0].Description.Value.ShouldBe("Red");
        if (parseComments)
            enumDef.Values[0].Comment.Value.ShouldBe(" comment 20");
        enumDef.Values[1].Name.Value.ShouldBe("BLUE");
        enumDef.Values[1].Description.Value.ShouldBe("Blue");
        if (parseComments)
            enumDef.Values[1].Comment.Value.ShouldBe(" comment 22");

        var inputDef = defs.Single(x => x is GraphQLInputObjectTypeDefinition) as GraphQLInputObjectTypeDefinition;
        inputDef.Name.Value.ShouldBe("TestInputObject");
        inputDef.Description.Value.ShouldBe("This is an example input object\nLine two of the description");
        if (parseComments)
            inputDef.Comment.Value.ShouldBe(" comment 24");
        inputDef.Fields.Count.ShouldBe(1);
        inputDef.Fields[0].Name.Value.ShouldBe("Value");
        inputDef.Fields[0].Type.ShouldBeAssignableTo<GraphQLNamedType>().Name.Value.ShouldBe("JSON");
        inputDef.Fields[0].Description.Value.ShouldBe("The value of the input object\n  (any JSON value is accepted)");
        if (parseComments)
            inputDef.Fields[0].Comment.Value.ShouldBe(" comment 26");

        var directiveDef = defs.Single(x => x is GraphQLDirectiveDefinition) as GraphQLDirectiveDefinition;
        directiveDef.Name.Value.ShouldBe("TestDirective");
        directiveDef.Description.Value.ShouldBe("Test directive");
        if (parseComments)
            directiveDef.Comment.Value.ShouldBe(" comment 28");
        directiveDef.Arguments.Count.ShouldBe(1);
        directiveDef.Arguments[0].Name.Value.ShouldBe("Value");
        directiveDef.Arguments[0].Type.ShouldBeAssignableTo<GraphQLNamedType>().Name.Value.ShouldBe("Int");
        directiveDef.Arguments[0].Description.Value.ShouldBe("Example");
        if (parseComments)
            directiveDef.Arguments[0].Comment.Value.ShouldBe(" comment 30");
    }

    [Theory]
    [InlineData(IgnoreOptions.None)]
    [InlineData(IgnoreOptions.Comments)]
    [InlineData(IgnoreOptions.Locations)]
    [InlineData(IgnoreOptions.All)]
    public void Descriptions_WithComments_Should_Read_Correctly_3(IgnoreOptions options)
    {
        var document = @"
# comment 0
""Super schema""
schema {
  query: String
}

# comment 1
""A JSON scalar""
scalar JSON

# comment 3
""""""
Human type
""""""
type Human {
  # comment 5
  """"""
  Name of human
  """"""
  name: String

  # comment 7
  ""Test""
  test(
    # comment 9
    ""desc""
    arg: Int
  ): Int
}

# comment 11
""Test interface""
interface TestInterface {
  # comment 13
  ""Object name""
  name: String
}

# comment 15
""""""
Test union
""""""
union TestUnion = Test1 | Test2

# comment 17
""Example enum""
enum Colors {
  # comment 19
  ""Red""
  RED
  # comment 21
  ""Blue""
  BLUE
}

# comment 23
""""""
This is an example input object
Line two of the description
""""""
input TestInputObject {
    # comment 25
    """"""
    The value of the input object
      (any JSON value is accepted)
    """"""
    Value: JSON
}

# comment 27
""Test directive""
directive @TestDirective (
  # comment 29
  ""Example""
  Value: Int
) on QUERY
".Parse(new ParserOptions { Ignore = options });
        var defs = document.Definitions;
        defs.Count.ShouldBe(8);
        var parseComments = !options.HasFlag(IgnoreOptions.Comments);

        var schemaDef = defs.Single(x => x is GraphQLSchemaDefinition) as GraphQLSchemaDefinition;
        schemaDef.Description.Value.ShouldBe("Super schema");
        if (parseComments)
            schemaDef.Comment.Value.ShouldBe(" comment 0");

        var scalarDef = defs.Single(x => x is GraphQLScalarTypeDefinition) as GraphQLScalarTypeDefinition;
        scalarDef.Name.Value.ShouldBe("JSON");
        scalarDef.Description.Value.ShouldBe("A JSON scalar");
        if (parseComments)
            scalarDef.Comment.Value.ShouldBe(" comment 1");

        var objectDef = defs.Single(x => x is GraphQLObjectTypeDefinition) as GraphQLObjectTypeDefinition;
        objectDef.Name.Value.ShouldBe("Human");
        objectDef.Description.Value.ShouldBe("Human type");
        if (parseComments)
            objectDef.Comment.Value.ShouldBe(" comment 3");
        objectDef.Fields.Count.ShouldBe(2);
        objectDef.Fields[0].Name.Value.ShouldBe("name");
        objectDef.Fields[0].Type.ShouldBeAssignableTo<GraphQLNamedType>().Name.Value.ShouldBe("String");
        objectDef.Fields[0].Description.Value.ShouldBe("Name of human");
        if (parseComments)
            objectDef.Fields[0].Comment.Value.ShouldBe(" comment 5");
        objectDef.Fields[1].Name.Value.ShouldBe("test");
        objectDef.Fields[1].Type.ShouldBeAssignableTo<GraphQLNamedType>().Name.Value.ShouldBe("Int");
        objectDef.Fields[1].Description.Value.ShouldBe("Test");
        if (parseComments)
            objectDef.Fields[1].Comment.Value.ShouldBe(" comment 7");
        objectDef.Fields[1].Arguments.Count.ShouldBe(1);
        objectDef.Fields[1].Arguments[0].Name.Value.ShouldBe("arg");
        objectDef.Fields[1].Arguments[0].Description.Value.ShouldBe("desc");
        objectDef.Fields[1].Arguments[0].Type.ShouldBeAssignableTo<GraphQLNamedType>().Name.Value.ShouldBe("Int");
        if (parseComments)
            objectDef.Fields[1].Arguments[0].Comment.Value.ShouldBe(" comment 9");

        var interfaceDef = defs.Single(x => x is GraphQLInterfaceTypeDefinition) as GraphQLInterfaceTypeDefinition;
        interfaceDef.Name.Value.ShouldBe("TestInterface");
        interfaceDef.Description.Value.ShouldBe("Test interface");
        if (parseComments)
            interfaceDef.Comment.Value.ShouldBe(" comment 11");
        interfaceDef.Fields.Count.ShouldBe(1);
        interfaceDef.Fields[0].Name.Value.ShouldBe("name");
        interfaceDef.Fields[0].Type.ShouldBeAssignableTo<GraphQLNamedType>().Name.Value.ShouldBe("String");
        interfaceDef.Fields[0].Description.Value.ShouldBe("Object name");
        if (parseComments)
            interfaceDef.Fields[0].Comment.Value.ShouldBe(" comment 13");

        var unionDef = defs.Single(x => x is GraphQLUnionTypeDefinition) as GraphQLUnionTypeDefinition;
        unionDef.Name.Value.ShouldBe("TestUnion");
        unionDef.Description.Value.ShouldBe("Test union");
        if (parseComments)
            unionDef.Comment.Value.ShouldBe(" comment 15");
        unionDef.Types.Count.ShouldBe(2);
        unionDef.Types[0].Name.Value.ShouldBe("Test1");
        unionDef.Types[1].Name.Value.ShouldBe("Test2");

        var enumDef = defs.Single(x => x is GraphQLEnumTypeDefinition) as GraphQLEnumTypeDefinition;
        enumDef.Name.Value.ShouldBe("Colors");
        enumDef.Description.Value.ShouldBe("Example enum");
        if (parseComments)
            enumDef.Comment.Value.ShouldBe(" comment 17");
        enumDef.Values.Count.ShouldBe(2);
        enumDef.Values[0].Name.Value.ShouldBe("RED");
        enumDef.Values[0].Description.Value.ShouldBe("Red");
        if (parseComments)
            enumDef.Values[0].Comment.Value.ShouldBe(" comment 19");
        enumDef.Values[1].Name.Value.ShouldBe("BLUE");
        enumDef.Values[1].Description.Value.ShouldBe("Blue");
        if (parseComments)
            enumDef.Values[1].Comment.Value.ShouldBe(" comment 21");

        var inputDef = defs.Single(x => x is GraphQLInputObjectTypeDefinition) as GraphQLInputObjectTypeDefinition;
        inputDef.Name.Value.ShouldBe("TestInputObject");
        inputDef.Description.Value.ShouldBe("This is an example input object\nLine two of the description");
        if (parseComments)
            inputDef.Comment.Value.ShouldBe(" comment 23");
        inputDef.Fields.Count.ShouldBe(1);
        inputDef.Fields[0].Name.Value.ShouldBe("Value");
        inputDef.Fields[0].Type.ShouldBeAssignableTo<GraphQLNamedType>().Name.Value.ShouldBe("JSON");
        inputDef.Fields[0].Description.Value.ShouldBe("The value of the input object\n  (any JSON value is accepted)");
        if (parseComments)
            inputDef.Fields[0].Comment.Value.ShouldBe(" comment 25");

        var directiveDef = defs.Single(x => x is GraphQLDirectiveDefinition) as GraphQLDirectiveDefinition;
        directiveDef.Name.Value.ShouldBe("TestDirective");
        directiveDef.Description.Value.ShouldBe("Test directive");
        if (parseComments)
            directiveDef.Comment.Value.ShouldBe(" comment 27");
        directiveDef.Arguments.Count.ShouldBe(1);
        directiveDef.Arguments[0].Name.Value.ShouldBe("Value");
        directiveDef.Arguments[0].Type.ShouldBeAssignableTo<GraphQLNamedType>().Name.Value.ShouldBe("Int");
        directiveDef.Arguments[0].Description.Value.ShouldBe("Example");
        if (parseComments)
            directiveDef.Arguments[0].Comment.Value.ShouldBe(" comment 29");
    }
}
