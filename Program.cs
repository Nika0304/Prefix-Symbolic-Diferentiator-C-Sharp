using System;
using System.Collections.Generic;
using System.Globalization;

class Program
{
    static bool IsNumber(string s, out double v)
        => double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v);

    static string DToStr(double v)
        => v.ToString(CultureInfo.InvariantCulture);

    static bool IsZero(object x) => x is string s && s == "0";
    static bool IsOne(object x) => x is string s && s == "1";
    static bool IsMinusOne(object x) => x is string s && s == "-1";

    static object Neg(object expr) 
    {
        // -(0) = 0
        if (IsZero(expr)) return "0";

        // -number => "-number"
        if (expr is string s && IsNumber(s, out double v))
            return DToStr(-v);

        // -(-u) = u
        if (expr is List<object> l && l.Count == 2 && (l[0] as string) == "-")
            return l[1];

        return new List<object> { "-", expr };
    }


    static object Derivator(object expr, string variable)
        => Simplify(DeriveRaw(expr, variable));

  
    static object DeriveRaw(object expr, string variable)
    {
        // 1) CONST / VAR
        if (expr is string str)
        {
            if (IsNumber(str, out _)) return "0";       // c' = 0
            return (str == variable) ? "1" : "0";       // x' = 1, y' = 0
        }

        // 2) compus
        if (expr is List<object> list)
        {
            string op = list[0] as string ?? throw new Exception("Operator invalid");

            //   count == 3
            if (list.Count == 3)
            {
                object a = list[1];
                object b = list[2];

                // (a +/- b)' = a' +/- b'
                if (op == "+" || op == "-")
                    return new List<object> { op, DeriveRaw(a, variable), DeriveRaw(b, variable) };

                // (a*b)' = a'*b + a*b'
                if (op == "*")
                {
                    return new List<object>
                    {
                        "+",
                        new List<object> { "*", DeriveRaw(a, variable), b },
                        new List<object> { "*", a, DeriveRaw(b, variable) }
                    };
                }

                // (a/b)' = (a'*b - a*b') / b^2
                if (op == "/")
                {
                    return new List<object>
                    {
                        "/",
                        new List<object>
                        {
                            "-",
                            new List<object> { "*", DeriveRaw(a, variable), b },
                            new List<object> { "*", a, DeriveRaw(b, variable) }
                        },
                        new List<object> { "^", b, "2" }
                    };
                }

                // Powers:
                if (op == "^")
                {
                    // u^n, n numeric const: (u^n)' = n*u^(n-1)*u'
                    if (b is string expStr && IsNumber(expStr, out double n))
                    {
                        if (Math.Abs(n) < 1e-12) return "0"; // u^0 constant
                        return new List<object>
                        {
                            "*",
                            new List<object> { "*", expStr, new List<object> { "^", a, DToStr(n - 1) } },
                            DeriveRaw(a, variable)
                        };
                    }

                    // e^u: (e^u)' = e^u * u'
                    if (a is string baseE && baseE == "e")
                        return new List<object> { "*", new List<object> { "^", "e", b }, DeriveRaw(b, variable) };

                    // c^u: (c^u)' = c^u * ln(c) * u'  (c numeric, c>0, c!=1)
                    if (a is string baseC && IsNumber(baseC, out double c) && c > 0.0 && c != 1.0)
                    {
                        return new List<object>
                        {
                            "*",
                            new List<object>
                            {
                                "*",
                                new List<object> { "^", a, b },
                                new List<object> { "ln", a }
                            },
                            DeriveRaw(b, variable)
                        };
                    }

                    throw new Exception("Putere neacoperita (astept exponent numeric sau baza e/const numerica).");
                }

                // log_a(u) as ["log", a, u], a numeric const >0, a!=1
                if (op == "log")
                {
                    if (a is string baseStr && IsNumber(baseStr, out double baseVal) && baseVal > 0.0 && baseVal != 1.0)
                    {
                        // (log_a(u))' = u' / (u * ln(a))
                        return new List<object>
                        {
                            "/",
                            DeriveRaw(b, variable),
                            new List<object> { "*", b, new List<object> { "ln", a } }
                        };
                    }
                    throw new Exception("Baza log trebuie sa fie constanta numerica >0 si !=1");
                }

                throw new Exception("Operator binar necunoscut: " + op);
            }

            //  unary (count == 2) 
            if (list.Count == 2)
            {
                object u = list[1];

                if (op == "-")
                    return Neg(DeriveRaw(u, variable));

                // sqrt(u): (sqrt(u))' = 1/(2*sqrt(u)) * u'
                if (op == "sqrt")
                {
                    return new List<object>
                    {
                        "*",
                        new List<object>
                        {
                            "/",
                            "1",
                            new List<object> { "*", "2", new List<object> { "sqrt", u } }
                        },
                        DeriveRaw(u, variable)
                    };
                }

                // ln(u): (ln(u))' = u'/u  => ln(x) -> ["/",1,"x"]
                if (op == "ln")
                    return new List<object> { "/", DeriveRaw(u, variable), u };

                // sin(u): cos(u) * u'
                if (op == "sin")
                    return new List<object> { "*", new List<object> { "cos", u }, DeriveRaw(u, variable) };

                // cos(u): -sin(u) * u'  
                if (op == "cos")
                    return new List<object> { "*", Neg(new List<object> { "sin", u }), DeriveRaw(u, variable) };

                // tg(u): 1/cos^2(u) * u'
                if (op == "tg")
                {
                    return new List<object>
                    {
                        "*",
                        new List<object>
                        {
                            "/",
                            "1",
                            new List<object> { "^", new List<object> { "cos", u }, "2" }
                        },
                        DeriveRaw(u, variable)
                    };
                }

                // ctg(u): -1/sin^2(u) * u'
                if (op == "ctg")
                {
                    return new List<object>
                    {
                        "*",
                        Neg(new List<object>
                        {
                            "/",
                            "1",
                            new List<object> { "^", new List<object> { "sin", u }, "2" }
                        }),
                        DeriveRaw(u, variable)
                    };
                }

                // arcsin(u): 1/sqrt(1-u^2) * u'
                if (op == "arcsin")
                {
                    return new List<object>
                    {
                        "*",
                        new List<object>
                        {
                            "/",
                            "1",
                            new List<object>
                            {
                                "sqrt",
                                new List<object> { "-", "1", new List<object> { "^", u, "2" } }
                            }
                        },
                        DeriveRaw(u, variable)
                    };
                }

                // arccos(u): -1/sqrt(1-u^2) * u'
                if (op == "arccos")
                {
                    return new List<object>
                    {
                        "*",
                        Neg(new List<object>
                        {
                            "/",
                            "1",
                            new List<object>
                            {
                                "sqrt",
                                new List<object> { "-", "1", new List<object> { "^", u, "2" } }
                            }
                        }),
                        DeriveRaw(u, variable)
                    };
                }

                // arctg(u): 1/(1+u^2) * u'
                if (op == "arctg")
                {
                    return new List<object>
                    {
                        "*",
                        new List<object>
                        {
                            "/",
                            "1",
                            new List<object> { "+", "1", new List<object> { "^", u, "2" } }
                        },
                        DeriveRaw(u, variable)
                    };
                }

                // arcctg(u): -1/(1+u^2) * u'
                if (op == "arcctg")
                {
                    return new List<object>
                    {
                        "*",
                        Neg(new List<object>
                        {
                            "/",
                            "1",
                            new List<object> { "+", "1", new List<object> { "^", u, "2" } }
                        }),
                        DeriveRaw(u, variable)
                    };
                }

                throw new Exception("Functie unara necunoscuta: " + op);
            }

            throw new Exception("Lista invalida: astept 2 (functie) sau 3 (operator).");
        }

        throw new Exception("Expresie neprocesata");
    }

    //Simplify the exp
    static object Simplify(object expr)
    {
        if (expr is string) return expr;

        if (expr is List<object> list)
        {
            string op = list[0] as string ?? throw new Exception("Op invalid");

            for (int i = 1; i < list.Count; i++)
                list[i] = Simplify(list[i]);

            // unary
            if (list.Count == 2)
            {
                object u = list[1];

                if (op == "-")
                    return Neg(u);

                return new List<object> { op, u };
            }

            // binary
            if (list.Count == 3)
            {
                object a = list[1];
                object b = list[2];

                if (op == "+")
                {
                    if (IsZero(a)) return b;
                    if (IsZero(b)) return a;
                }

                if (op == "-")
                {
                    if (IsZero(b)) return a;
                    if (IsZero(a)) return Neg(b);
                }

                if (op == "*")
                {
                    if (IsZero(a) || IsZero(b)) return "0";
                    if (IsOne(a)) return b;
                    if (IsOne(b)) return a;

                    if (IsMinusOne(a)) return Neg(b);
                    if (IsMinusOne(b)) return Neg(a);

                    // (-u)*v => -(u*v)  
                    if (a is List<object> la && la.Count == 2 && (la[0] as string) == "-")
                        return Neg(Simplify(new List<object> { "*", la[1], b }));
                    if (b is List<object> lb && lb.Count == 2 && (lb[0] as string) == "-")
                        return Neg(Simplify(new List<object> { "*", a, lb[1] }));
                }

                if (op == "/")
                {
                    if (IsZero(a)) return "0";
                    if (IsOne(b)) return a;

                    // (-u)/v => -(u/v)
                    if (a is List<object> la && la.Count == 2 && (la[0] as string) == "-")
                        return Neg(new List<object> { "/", la[1], b });
                }

                if (op == "^")
                {
                    if (b is string sb1 && sb1 == "1") return a;
                    if (b is string sb0 && sb0 == "0") return "1";
                    if (a is string sa1 && sa1 == "1") return "1";
                    if (a is string sa0 && sa0 == "0") return "0";
                }

                return new List<object> { op, a, b };
            }

            throw new Exception("Lista invalida in Simplify (astept 2 sau 3).");
        }

        throw new Exception("Expresie invalida");
    }

   
    static void PrintExpr(object expr)
    {
        if (expr is string s)
        {
            if (IsNumber(s, out _)) Console.Write(s);
            else Console.Write("\"" + s + "\"");
            return;
        }

        if (expr is List<object> list)
        {
            Console.Write("[");
            for (int i = 0; i < list.Count; i++)
            {
                PrintExpr(list[i]);
                if (i < list.Count - 1) Console.Write(",");
            }
            Console.Write("]");
            return;
        }

        throw new Exception("Nu pot afisa expresia.");
    }


    static void RunTest(string name, object expr, string variable)
    {
        Console.WriteLine("\n----------------");
        Console.Write("expr = ");
        PrintExpr(expr);
        Console.WriteLine();

        object d = Derivator(expr, variable);
        Console.Write("derivator(expr) = ");
        PrintExpr(d);
        Console.WriteLine();
    }

    //pentru citirea de la tastatura
    class PrefixParser
    {
        private readonly string str;
        private int i;

        public PrefixParser(string s) { str = s; i = 0; }

        void SkipWs()
        {
            while (i < str.Length && char.IsWhiteSpace(str[i])) i++;
        }

        char Peek()
        {
            SkipWs();
            return i < str.Length ? str[i] : '\0';
        }

        char Next()
        {
            SkipWs();
            return i < str.Length ? str[i++] : '\0';
        }

        public object ParseExpr()
        {
            SkipWs();
            char c = Peek();

            if (c == '[') return ParseList();
            return ParseAtom();
        }

        object ParseList()
        {
            if (Next() != '[') throw new Exception("Lipseste '['");

            var list = new List<object>();

            SkipWs();
            if (Peek() == ']') { Next(); return list; } // lista goala 

            list.Add(ParseExpr());

            SkipWs();
            while (Peek() == ',')
            {
                Next(); // dam skip la  ','
                list.Add(ParseExpr());
                SkipWs();
            }

            if (Next() != ']') throw new Exception("Lipseste ']'");
            return list;
        }

        object ParseAtom()
        {
            SkipWs();
            char c = Peek();

            // "x" or 'x'
            if (c == '"' || c == '\'')
            {
                char q = Next();
                int start = i;
                while (i < str.Length && str[i] != q) i++;
                if (i >= str.Length) throw new Exception("String neinchis cu ghilimele");
                string token = str.Substring(start, i - start);
                i++; 
                return token;
            }

            
            int st = i;
            while (i < str.Length)
            {
                char ch = str[i];
                if (char.IsWhiteSpace(ch) || ch == ',' || ch == ']') break;
                i++;
            }

            if (i == st) throw new Exception("Token lipsa");
            string tok = str.Substring(st, i - st);
            return tok;
        }
    }

    static object ReadExprFromConsole()
    {
        Console.WriteLine("Introdu expresia in prefix-list, ex:");
        Console.WriteLine("  [\"sin\",\"x\"]");
        Console.WriteLine("  [\"sin\",[\"cos\",\"x\"]]");
        Console.WriteLine("  [\"^\",\"x\",2]");
        Console.Write("> ");
        string line = Console.ReadLine() ?? "";
        var p = new PrefixParser(line);
        object expr = p.ParseExpr();
        return expr;
    }

    static void RunAllTests(string variable)
    {
        RunTest("TABEL 1: c (constanta)", "7.5", variable);

        RunTest("TABEL 2: x", "x", variable);
        RunTest("Variabila diferita (y)", "y", variable);

        RunTest("Regula: +", new List<object> { "+", "x", "2" }, variable);
        RunTest("Regula: -", new List<object> { "-", "2", "x" }, variable);
        RunTest("Regula: *", new List<object> { "*", "x", "x" }, variable);
        RunTest("Regula: /", new List<object> { "/", "x", "2" }, variable);

        RunTest("TABEL 3: x^a", new List<object> { "^", "x", "5" }, variable);
        RunTest("TABEL 3 (compus): (x+1)^3", new List<object> { "^", new List<object> { "+", "x", "1" }, "3" }, variable);

        RunTest("TABEL 4: 1/x", new List<object> { "/", "1", "x" }, variable);

        RunTest("TABEL 5: sqrt(x)", new List<object> { "sqrt", "x" }, variable);
        RunTest("TABEL 5 (compus): sqrt(x^2)", new List<object> { "sqrt", new List<object> { "^", "x", "2" } }, variable);

        RunTest("TABEL 6: e^x", new List<object> { "^", "e", "x" }, variable);
        RunTest("TABEL 6 (compus): e^(x+1)", new List<object> { "^", "e", new List<object> { "+", "x", "1" } }, variable);

        RunTest("TABEL 7: 2^x", new List<object> { "^", "2", "x" }, variable);
        RunTest("TABEL 7 (compus): 2^(3*x+2)",
            new List<object> { "^", "2", new List<object> { "+", new List<object> { "*", "3", "x" }, "2" } }, variable);

        RunTest("TABEL 8: ln(x)", new List<object> { "ln", "x" }, variable);
        RunTest("TABEL 8 (compus): ln(3*x+5)",
            new List<object> { "ln", new List<object> { "+", new List<object> { "*", "3", "x" }, "5" } }, variable);

        RunTest("TABEL 9: log_2(x)", new List<object> { "log", "2", "x" }, variable);
        RunTest("TABEL 9 (compus): log_3(x^2+2*x)",
            new List<object>
            {
                "log","3",
                new List<object> { "+", new List<object> { "^","x","2" }, new List<object> { "*","2","x" } }
            }, variable);

        RunTest("TABEL 10: sin(x)", new List<object> { "sin", "x" }, variable);
        RunTest("TABEL 10 (compus): sin(ln(x+2))",
            new List<object> { "sin", new List<object> { "ln", new List<object> { "+", "x", "2" } } }, variable);

        RunTest("TABEL 11: cos(x)", new List<object> { "cos", "x" }, variable);
        RunTest("TABEL 11 (compus): cos(sqrt(x+1))",
            new List<object> { "cos", new List<object> { "sqrt", new List<object> { "+", "x", "1" } } }, variable);

        RunTest("TABEL 12: tg(x)", new List<object> { "tg", "x" }, variable);
        RunTest("TABEL 12 (compus): tg((x+1)^2)",
            new List<object> { "tg", new List<object> { "^", new List<object> { "+", "x", "1" }, "2" } }, variable);

        RunTest("TABEL 13: ctg(x)", new List<object> { "ctg", "x" }, variable);
        RunTest("TABEL 13 (compus): ctg(ln(x+1))",
            new List<object> { "ctg", new List<object> { "ln", new List<object> { "+", "x", "1" } } }, variable);

        RunTest("TABEL 14: arcsin(x)", new List<object> { "arcsin", "x" }, variable);
        RunTest("TABEL 14 (compus): arcsin(sin(x+1))",
            new List<object> { "arcsin", new List<object> { "sin", new List<object> { "+", "x", "1" } } }, variable);

        RunTest("TABEL 15: arccos(x)", new List<object> { "arccos", "x" }, variable);
        RunTest("TABEL 15 (compus): arccos(cos(x-1))",
            new List<object> { "arccos", new List<object> { "cos", new List<object> { "-", "x", "1" } } }, variable);

        RunTest("TABEL 16: arctg(x)", new List<object> { "arctg", "x" }, variable);
        RunTest("TABEL 16 (compus): arctg((x+1)/2)",
            new List<object> { "arctg", new List<object> { "/", new List<object> { "+", "x", "1" }, "2" } }, variable);

        RunTest("TABEL 17: arcctg(x)", new List<object> { "arcctg", "x" }, variable);
        RunTest("TABEL 17 (compus): arcctg(sqrt(x+1))",
            new List<object> { "arcctg", new List<object> { "sqrt", new List<object> { "+", "x", "1" } } }, variable);

        RunTest("EXEMPLU CERINTA: sin(cos(x))",
            new List<object> { "sin", new List<object> { "cos", "x" } }, variable);

        object poly = new List<object>
        {
            "+",
            new List<object>
            {
                "+",
                new List<object> { "*", "3", new List<object> { "^", "x", "3" } },
                new List<object> { "*", "2", new List<object> { "^", "x", "2" } }
            },
            new List<object>
            {
                "+",
                new List<object> { "*", "5", "x" },
                "7"
            }
        };
        RunTest("EXEMPLU CERINTA: 3*x^3 + 2*x^2 + 5*x + 7", poly, variable);
    }

    static void Main()
    {
        Console.Write("Variabila de derivare: ");
        string variable = Console.ReadLine() ?? throw new Exception("Variabila nu poate fi null");

        while (true)
        {
            Console.WriteLine("\n--------");
            Console.WriteLine("1) Ruleaza toate testele");
            Console.WriteLine("2) Citeste expresie de la tastatura si deriva");
            Console.WriteLine("0) Iesire");
            Console.Write("> ");
            string opt = (Console.ReadLine() ?? "").Trim();

            if (opt == "0") break;

            if (opt == "1")
            {
                RunAllTests(variable);
            }
            else if (opt == "2")
            {
                try
                {
                    object expr = ReadExprFromConsole();
                    RunTest("CUSTOM INPUT", expr, variable);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Eroare la parsare/derivare: " + ex.Message);
                }
            }
            else
            {
                Console.WriteLine("Optiune invalida.");
            }
        }
    }
}

