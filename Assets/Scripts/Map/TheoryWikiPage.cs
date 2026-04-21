namespace Map.Wiki
{
    /// <summary>
    /// Páginas de teoria da wiki (abas <c>WikiTab_*</c> / <c>WikiPage_*</c>).
    /// O valor por defeito (<see cref="Overview"/>) corresponde à visão geral.
    /// </summary>
    public enum TheoryWikiPage
    {
        /// <summary>Visão geral — <c>WikiTab_overview</c>.</summary>
        Overview = 0,
        /// <summary>Adição — <c>WikiTab_math_add</c>.</summary>
        Addition = 1,
        /// <summary>Subtração — <c>WikiTab_math_sub</c>.</summary>
        Subtraction = 2,
        /// <summary>Multiplicação — <c>WikiTab_math_mul</c>.</summary>
        Multiplication = 3,
        /// <summary>Divisão — <c>WikiTab_math_div</c>.</summary>
        Division = 4,
    }

    public static class TheoryWikiPageMapping
    {
        public const string TabOverview = "overview";
        public const string TabMathAdd = "math_add";
        public const string TabMathSub = "math_sub";
        public const string TabMathMul = "math_mul";
        public const string TabMathDiv = "math_div";

        public static TheoryWikiPage FromMapNodeType(MapNodeType type)
        {
            return type switch
            {
                MapNodeType.Combat_Add => TheoryWikiPage.Addition,
                MapNodeType.Combat_Sub => TheoryWikiPage.Subtraction,
                MapNodeType.Combat_Multi => TheoryWikiPage.Multiplication,
                MapNodeType.Combat_Div => TheoryWikiPage.Division,
                _ => TheoryWikiPage.Overview,
            };
        }

        public static string ToTabId(TheoryWikiPage page)
        {
            return page switch
            {
                TheoryWikiPage.Overview => TabOverview,
                TheoryWikiPage.Addition => TabMathAdd,
                TheoryWikiPage.Subtraction => TabMathSub,
                TheoryWikiPage.Multiplication => TabMathMul,
                TheoryWikiPage.Division => TabMathDiv,
                _ => TabOverview,
            };
        }

        /// <summary>Nome do tipo, para o rótulo do "Não mostrar novamente para &lt;tipo&gt;".</summary>
        public static string DisplayName(TheoryWikiPage page)
        {
            return page switch
            {
                TheoryWikiPage.Addition => "Adição",
                TheoryWikiPage.Subtraction => "Subtração",
                TheoryWikiPage.Multiplication => "Multiplicação",
                TheoryWikiPage.Division => "Divisão",
                _ => "Visão geral",
            };
        }
    }
}
