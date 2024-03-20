// TODOs:
// - int64 literals (e.g. `0L`)
// - deal with keywords
// - better support for partially-written code (i.e. `let x =`)

const PREC = {
  LOGICAL_OR: 0,
  LOGICAL_AND: 1,
  COMPARISON: 2,
  SUM: 3,
  PRODUCT: 4,
  EXPONENT: 5,
};

const logicalOperators = choice("&&", "||");
const comparisonOperators = choice("==", "!=", "<", "<=", ">", ">=");
const additiveOperators = choice("+", "-");
const multiplicativeOperators = choice("*", "/", "%");
const exponentOperator = "^";

module.exports = grammar({
  name: "darklang",

  externals: $ => [$.DOC_COMMENT],

  rules: {
    source_file: $ =>
      seq(
        // all type and fn defs first
        repeat(choice($.type_decl, $.fn_decl, $.DOC_COMMENT)),

        // then the expressions to evaluate, in order
        repeat($.expression),
      ),

    //
    // Function declarations
    fn_decl: $ =>
      seq(
        field("keyword_let", alias("let", $.keyword)),
        field("name", $.fn_identifier),
        field("params", $.fn_decl_params),
        field("symbol_colon", alias(":", $.symbol)),
        field("return_type", $.type_reference),
        field("symbol_equals", alias("=", $.symbol)),
        field("body", $.expression),
      ),
    fn_decl_params: $ => repeat1(choice($.unit, $.fn_decl_param)),
    fn_decl_param: $ =>
      seq(
        field("symbol_left_paren", alias("(", $.symbol)),
        field("identifier", $.variable_identifier),
        field("symbol_colon", alias(":", $.symbol)),
        field("typ", $.type_reference),
        field("symbol_right_paren", alias(")", $.symbol)),
      ),

    //
    // Type declarations
    type_decl: $ =>
      seq(
        field("keyword_type", alias("type", $.keyword)),
        field("name", $.type_identifier),
        field("symbol_equals", alias("=", $.symbol)),
        field("typ", $.type_reference),
      ),

    //
    // Expressions
    expression: $ =>
      choice(
        $.paren_expression,
        $.unit,
        $.bool_literal,
        $.int8_literal,
        $.uint8_literal,
        $.int16_literal,
        $.uint16_literal,
        $.int32_literal,
        $.uint32_literal,
        $.int64_literal,
        $.uint64_literal,
        $.int128_literal,
        $.uint128_literal,
        $.float_literal,
        $.string_literal,
        $.char_literal,

        $.let_expression,
        $.variable_identifier,

        $.infix_operation,
        $.function_call,
      ),
    paren_expression: $ =>
      seq(
        field("symbol_left_paren", alias("(", $.symbol)),
        field("expr", $.expression),
        field("symbol_right_paren", alias(")", $.symbol)),
      ),

    bool_literal: $ => choice(/true/, /false/),

    /**
     * e.g. `Int64.add 1 2
     *
     * This is currently a bit hacky, requiring parens around the function call,
     * in order to help tree-sitter parse it correctly.
     *
     * There's certainly a better way to remove ambiguities. TODO
     *
     * TODO maybe call this "apply" instead
     *      sometimes the thing we are 'applying' is not directly a function
     */
    function_call: $ =>
      seq(
        field("symbol_left_paren", alias("(", $.symbol)),
        field("fn", $.qualified_fn_name),
        field("args", repeat1($.expression)),
        field("symbol_right_paren", alias(")", $.symbol)),
      ),

    let_expression: $ =>
      seq(
        field("keyword_let", alias("let", $.keyword)),
        field("identifier", $.variable_identifier),
        field("symbol_equals", alias("=", $.symbol)),
        field("expr", $.expression),
        "\n",
        field("body", $.expression),
      ),

    //
    // Strings
    // TODO: maybe add support for multiline strings (""")
    string_literal: $ =>
      choice(
        seq(
          field("symbol_open_quote", alias('"', $.symbol)),
          field("symbol_close_quote", alias('"', $.symbol)),
        ),
        seq(
          field("symbol_open_quote", alias('"', $.symbol)),
          field("content", $.string_content),
          field("symbol_close_quote", alias('"', $.symbol)),
        ),
      ),
    string_content: $ =>
      repeat1(
        choice(
          // higher precedence than escape sequences
          token.immediate(prec(1, /[^\\"\n]+/)),
          $.char_or_string_escape_sequence,
        ),
      ),

    //
    // Characters
    char_literal: $ =>
      seq(
        field("symbol_open_single_quote", alias("'", $.symbol)),
        field("content", $.character),
        field("symbol_close_single_quote", alias("'", $.symbol)),
      ),
    character: $ =>
      choice(
        // higher precedence than escape sequences
        token.immediate(prec(1, /[^'\\\n]/)),
        $.char_or_string_escape_sequence,
      ),

    char_or_string_escape_sequence: _ =>
      token.immediate(seq("\\", /(\"|\\|\/|b|f|n|r|t|u)/)),

    //
    // Infix operations
    infix_operation: $ =>
      // given `1 + 2 * 3`, this will parse as `1 + (2 * 3)`
      choice(
        // Power
        prec.right(
          PREC.EXPONENT,
          seq(
            field("left", $.expression),
            field("operator", alias(exponentOperator, $.operator)),
            field("right", $.expression),
          ),
        ),

        // multiplication, division, modulo
        prec.left(
          PREC.PRODUCT,
          seq(
            field("left", $.expression),
            field("operator", alias(multiplicativeOperators, $.operator)),
            field("right", $.expression),
          ),
        ),

        // addition, subtraction
        prec.left(
          PREC.SUM,
          seq(
            field("left", $.expression),
            field("operator", alias(additiveOperators, $.operator)),
            field("right", $.expression),
          ),
        ),

        // Comparison
        prec.left(
          PREC.COMPARISON,
          seq(
            field("left", $.expression),
            field("operator", alias(comparisonOperators, $.operator)),
            field("right", $.expression),
          ),
        ),

        // Logical operations
        prec.left(
          PREC.LOGICAL_AND,
          seq(
            field("left", $.expression),
            field("operator", alias(logicalOperators, $.operator)),
            field("right", $.expression),
          ),
        ),
        prec.left(
          PREC.LOGICAL_OR,
          seq(
            field("left", $.expression),
            field("operator", alias(logicalOperators, $.operator)),
            field("right", $.expression),
          ),
        ),
      ),

    //
    // Integers
    //CLEANUP: we are using .NET suffixes for integers (e.g. `1L` for Int64) temporarily until we remove the old parser.
    int8_literal: $ =>
      seq(field("digits", $.digits), field("suffix", alias("y", $.symbol))),

    uint8_literal: $ =>
      seq(
        field("digits", $.positive_digits),
        field("suffix", alias("uy", $.symbol)),
      ),

    int16_literal: $ =>
      seq(field("digits", $.digits), field("suffix", alias("s", $.symbol))),

    uint16_literal: $ =>
      seq(
        field("digits", $.positive_digits),
        field("suffix", alias("us", $.symbol)),
      ),

    int32_literal: $ =>
      seq(field("digits", $.digits), field("suffix", alias("l", $.symbol))),

    uint32_literal: $ =>
      seq(
        field("digits", $.positive_digits),
        field("suffix", alias("ul", $.symbol)),
      ),

    int64_literal: $ =>
      seq(field("digits", $.digits), field("suffix", alias("L", $.symbol))),

    uint64_literal: $ =>
      seq(
        field("digits", $.positive_digits),
        field("suffix", alias("UL", $.symbol)),
      ),

    int128_literal: $ =>
      seq(field("digits", $.digits), field("suffix", alias("Q", $.symbol))),

    uint128_literal: $ =>
      seq(
        field("digits", $.positive_digits),
        field("suffix", alias("Z", $.symbol)),
      ),

    digits: $ => choice($.positive_digits, $.negative_digits),
    negative_digits: $ => /-\d+/,
    positive_digits: $ => /\d+/,

    //
    // Floats
    float_literal: $ => /[+-]?[0-9]+\.[0-9]+/,

    //
    // Common
    type_reference: $ => choice($.builtin_type, $.qualified_type_name),

    builtin_type: $ =>
      choice(
        /Unit/,
        /Bool/,
        /Int8/,
        /UInt8/,
        /Int16/,
        /UInt16/,
        /Int32/,
        /UInt32/,
        /Int64/,
        /UInt64/,
        /Int128/,
        /UInt128/,
        /Float/,
        /Char/,
        /String/,
        /DateTime/,
        /Uuid/,
      ),

    //
    // Identifiers
    qualified_fn_name: $ =>
      seq(
        repeat(seq($.module_identifier, alias(".", $.symbol))),
        $.fn_identifier,
      ),

    qualified_type_name: $ =>
      seq(
        repeat(seq($.module_identifier, alias(".", $.symbol))),
        $.type_identifier,
      ),

    /** e.g. `x` in `let double (x: Int) = x + x`
     *
     * for let bindings, params, etc. */
    variable_identifier: $ => /[a-z][a-zA-Z0-9_]*/,

    // e.g. `double` in `let double (x: Int) = x + x`
    fn_identifier: $ => /[a-z][a-zA-Z0-9_]*/,

    // e.g. `Person` in `type MyPerson = ...`
    type_identifier: $ => /[A-Z][a-zA-Z0-9_]*/,

    // e.g. `LanguageTools in `PACKAGE.Darklang.LanguageTools.SomeType`
    module_identifier: $ => /[A-Z][a-zA-Z0-9_]*/,

    unit: $ => "()",
  },
});
