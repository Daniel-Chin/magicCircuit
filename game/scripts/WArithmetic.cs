using System;
using System.IO;

using System.Linq;
using System.Text;

// every number >= 1

public enum TerminalType
{
    NUMBER, OMEGA
}

public class Terminal
{
    public static readonly Terminal w = new Terminal(TerminalType.OMEGA);

    public TerminalType MyTerminalType;
    public double MyNumber;

    public Terminal(TerminalType terminalType)
    {
        MyTerminalType = terminalType;
    }
    public Terminal(double number)
    {
        MyTerminalType = TerminalType.NUMBER;
        MyNumber = number;
    }
    public override string ToString()
    {
        switch (MyTerminalType)
        {
            case TerminalType.NUMBER:
                return MyNumber.ToString("#.#");
            case TerminalType.OMEGA:
                return "w";
            default:
                throw new Shared.ObjectStateIllegal();
        }
    }
}

public enum Operator
{
    PLUS, MINUS, TIMES, POWER
    // The order must represent precedence.  
}

public class Expression
{
    public static readonly Expression ONE = new Expression(new Terminal(1));
    public static readonly Expression MINUS_ONE = new Expression(new Terminal(-1));
    public static readonly Expression w = new Expression(Terminal.w);

    public bool IsTerminal;
    public Terminal MyTerminal;
    public Operator MyOperator;
    public Expression Left;
    public Expression Right;

    public Expression(Terminal terminal)
    {
        IsTerminal = true;
        MyTerminal = terminal;
    }

    public Expression(
        Expression left, Operator op, Expression right
    )
    {
        IsTerminal = false;
        Left = left;
        MyOperator = op;
        Right = right;
    }

    public override string ToString()
    {
        StringBuilder sB = new StringBuilder();
        BuildString(sB, null, false);
        return sB.ToString();
    }
    public void BuildString(
        StringBuilder sB, Operator? parentOperator,
        bool isLeftNotRight
    )
    {
        if (IsTerminal)
        {
            sB.Append(MyTerminal.ToString());
            return;
        }
        bool doParenthesis = false;
        if (parentOperator != null)
        {
            if (
                parentOperator > MyOperator
                || parentOperator == Operator.POWER
                && isLeftNotRight
            )
            {
                doParenthesis = true;
            }
        }
        if (doParenthesis) sB.Append('(');  // else sB.Append('<');
        Left.BuildString(sB, MyOperator, true);
        sB.Append(' ');
        sB.Append(OperatorToChar(MyOperator));
        sB.Append(' ');
        Right.BuildString(sB, MyOperator, false);
        if (doParenthesis) sB.Append(')');  // else sB.Append('>');
    }
    static public char OperatorToChar(Operator op)
    {
        switch (op)
        {
            case Operator.PLUS:
                return '+';
            case Operator.TIMES:
                return '*';
            case Operator.POWER:
                return '^';
            default:
                throw new Shared.ObjectStateIllegal();
        }
    }
    public void ExpandAll()
    {
        if (IsTerminal) return;
        Left.ExpandAll();
        Right.ExpandAll();
        if (MyOperator == Operator.TIMES)
        {
            if (!Left.IsTerminal && Left.MyOperator == Operator.PLUS)
            {
                (Left, Right) = (Right, Left);
            }
            if (!Right.IsTerminal && Right.MyOperator == Operator.PLUS)
            {
                (MyOperator, Left, Right) = (
                    Operator.PLUS,
                    new Expression(Left, Operator.TIMES, Right.Left),
                    new Expression(Left, Operator.TIMES, Right.Right)
                );
                Left.ExpandAll();
                Right.ExpandAll();
            }
        }
    }

    public Expression[] Top(Operator op)
    {
        if (!IsTerminal)
        {
            if (MyOperator == op)
            {
                return Left.Top(op).Concat(Right.Top(op)).ToArray();
            }
            else
            {
                Shared.Assert(MyOperator > op);
            }
        }
        return new Expression[] { this };
    }

    public static Expression Sample(double probToTerminate)
    {
        // Shared.Assert(probToTerminate > .5);    // converge
        if (Shared.Rand.NextDouble() < probToTerminate)
        {
            if (Shared.Rand.NextDouble() < .6)
            {
                if (Shared.Rand.NextDouble() < .3)
                {
                    return Expression.ONE;
                }
                return new Expression(new Terminal(
                    Shared.Rand.NextDouble() * 10 + 1
                ));
            }
            else
            {
                return Expression.w;
            }
        }
        else
        {
            double x = Shared.Rand.NextDouble();
            Operator op;
            if (x < .4)
            {
                op = Operator.PLUS;
            }
            else if (x < .7)
            {
                op = Operator.TIMES;
            }
            else
            {
                op = Operator.POWER;
            }
            probToTerminate += (1 - probToTerminate) * .3;
            return new Expression(
                Sample(probToTerminate), op,
                Sample(probToTerminate)
            );
        }
    }
}

public enum Rank
{
    FINITE, W_TO_THE_K, TWO_TO_THE_W, STACK_W
}

public class Simplest : JSONable
{   // immutable
    public Rank MyRank { get; set; }
    public double K { get; set; }
    public Simplest(
        Rank rank, double k
    )
    {
        MyRank = rank;
        K = k;
        switch (K)
        {
            case 0:
                switch (rank)
                {
                    case Rank.W_TO_THE_K:
                        MyRank = Rank.FINITE;
                        K = 1;
                        break;
                    case Rank.STACK_W:
                        throw new Shared.ObjectStateIllegal();
                }
                break;
            case 1:
                switch (rank)
                {
                    case Rank.STACK_W:
                        MyRank = Rank.W_TO_THE_K;
                        K = 1;
                        break;
                }
                break;
        }
        if (MyRank == Rank.TWO_TO_THE_W)
            Shared.Assert(K == -1);
    }
    public static bool operator <=(Simplest a, Simplest b)
    {
        if (a.MyRank < b.MyRank) return true;
        if (a.MyRank > b.MyRank) return false;
        return a.K <= b.K;
    }
    public static bool operator >=(Simplest a, Simplest b)
    {
        if (a.MyRank > b.MyRank) return true;
        if (a.MyRank < b.MyRank) return false;
        return a.K >= b.K;
    }
    public static Simplest FromExpression(
        Expression expression, bool verbose
    )
    {
        if (expression.IsTerminal)
        {
            TerminalType terminalType = expression.MyTerminal.MyTerminalType;
            if (terminalType == TerminalType.NUMBER)
                return new Simplest(Rank.FINITE, expression.MyTerminal.MyNumber);
            if (terminalType == TerminalType.OMEGA)
                return Simplest.W();
        }
        Simplest left = FromExpression(expression.Left, verbose);
        Simplest right = FromExpression(expression.Right, verbose);
        if (verbose)
        {
            Console.Write(left);
            Console.Write(' ');
            Console.Write(Expression.OperatorToChar(expression.MyOperator));
            Console.Write(' ');
            Console.Write(right);
            Console.WriteLine();
        }

        Simplest result = Eval(
            left, expression.MyOperator, right
        );
        if (verbose)
        {
            Console.Write(" = ");
            Console.WriteLine(result);
        }
        return result;
    }
    public static Simplest Eval(
        Simplest left, Operator op, Simplest right
    )
    {
        Simplest a; Simplest b;
        if (left <= right)
        {
            a = left; b = right;
        }
        else
        {
            b = left; a = right;
        }
        switch (op)
        {
            case Operator.PLUS:
                if (a.MyRank < b.MyRank)
                    return b;
                if (a.MyRank == Rank.FINITE)
                    return new Simplest(
                        Rank.FINITE, a.K + b.K
                    );
                return b;
            case Operator.MINUS:
                if (left.MyRank > right.MyRank)
                    return left;
                if (left.MyRank == right.MyRank)
                {
                    if (a.MyRank == Rank.FINITE)
                        return new Simplest(
                            Rank.FINITE, left.K - right.K
                        );
                    if (left.K > right.K)
                        return left;
                }
                return Simplest.Zero();
            case Operator.TIMES:
                switch (a.MyRank)
                {
                    case Rank.FINITE:
                        if (b.MyRank == Rank.FINITE)
                            return new Simplest(
                                Rank.FINITE, a.K * b.K
                            );
                        if (a.K == 0)
                            return a;
                        return b;
                    case Rank.W_TO_THE_K:
                        if (b.MyRank == Rank.W_TO_THE_K)
                            return new Simplest(
                                Rank.W_TO_THE_K, a.K + b.K
                            );
                        return b;
                    case Rank.TWO_TO_THE_W:
                        // this is mathematically wrong, but allows w^w^w to be easily created
                        if (
                            b.MyRank == Rank.TWO_TO_THE_W
                            || b.MyRank == Rank.STACK_W
                            && b.K == 2
                        )
                            return new Simplest(
                                Rank.STACK_W, 3
                            );
                        return b;
                    case Rank.STACK_W:
                        // this is mathematically wrong, but allows w^w^w to be easily created
                        if (a.K == b.K)
                            return new Simplest(
                                Rank.STACK_W, a.K + 1
                            );
                        return b;
                }
                break;
            case Operator.POWER:
                switch (left.MyRank)
                {
                    case Rank.FINITE:
                        if (right.MyRank == Rank.FINITE)
                            return new Simplest(
                                Rank.FINITE, Math.Pow(left.K, right.K)
                            );
                        if (left.K == 1)
                            return left;
                        if (right.MyRank == Rank.W_TO_THE_K)
                        {
                            if (right.K == 1)
                                return new Simplest(
                                    Rank.TWO_TO_THE_W, -1
                                );
                            return new Simplest(
                                Rank.STACK_W, 3
                            );
                        }
                        if (right.MyRank == Rank.TWO_TO_THE_W)
                            return new Simplest(
                                Rank.STACK_W, 3
                            );
                        if (right.MyRank == Rank.STACK_W)
                            return new Simplest(
                                Rank.STACK_W, right.K + 1
                            );
                        break;
                    case Rank.W_TO_THE_K:
                        if (right.MyRank == Rank.FINITE)
                            return new Simplest(
                                Rank.W_TO_THE_K, Math.Ceiling(left.K * right.K)
                            );
                        if (right.MyRank == Rank.W_TO_THE_K)
                        {
                            if (right.K == 1)
                                return new Simplest(
                                    Rank.STACK_W, 2
                                );
                            return new Simplest(
                                Rank.STACK_W, 3
                            );
                        }
                        if (right.MyRank == Rank.TWO_TO_THE_W)
                            return new Simplest(
                                Rank.STACK_W, 3
                            );
                        if (right.MyRank == Rank.STACK_W)
                            return new Simplest(
                                Rank.STACK_W, right.K + 1
                            );
                        break;
                    case Rank.TWO_TO_THE_W:
                        if (right.MyRank == Rank.FINITE)
                            return left;
                        if (right.MyRank == Rank.W_TO_THE_K)
                            return new Simplest(
                                Rank.STACK_W, 3
                            );
                        if (right.MyRank == Rank.TWO_TO_THE_W)
                            return new Simplest(
                                Rank.STACK_W, 3
                            );
                        if (right.MyRank == Rank.STACK_W)
                            return new Simplest(
                                Rank.STACK_W, right.K + 1
                            );
                        break;
                    case Rank.STACK_W:
                        if (right.MyRank == Rank.STACK_W)
                            return new Simplest(
                                Rank.STACK_W, Math.Max(
                                    left.K, right.K + 1
                                )
                            );
                        if (right.MyRank == Rank.FINITE)
                            return left;
                        return new Simplest(
                            Rank.STACK_W, Math.Max(
                                3, left.K
                            )
                        );
                }
                break;
        }
        throw new Shared.ObjectStateIllegal();
    }

    public override bool Equals(object obj)
    {
        if (!(obj is Simplest that)) return false;
        return (
            MyRank == that.MyRank
            && K == that.K
        );
    }
    public override int GetHashCode()
    {
        return MyRank.GetHashCode() ^ K.GetHashCode();
    }

    public override string ToString()
    {
        switch (MyRank)
        {
            case Rank.FINITE:
                return K.ToString("#.#");
            case Rank.W_TO_THE_K:
                if (K == 1)
                    return "w";
                return $"w^{K}";
            case Rank.TWO_TO_THE_W:
                return "2^w";
            case Rank.STACK_W:
                return String.Join("", Enumerable.Repeat("w^", ((int)K) - 1)) + "w";
            default:
                throw new Shared.ObjectStateIllegal();
        }
    }

    public static Simplest Zero()
    {
        return new Simplest(Rank.FINITE, 0);
    }
    public static Simplest One()
    {
        return new Simplest(Rank.FINITE, 1);
    }
    public static Simplest[] Zeros(int len)
    {
        Simplest[] x = new Simplest[len];
        for (int i = 0; i < len; i++)
        {
            x[i] = Zero();
        }
        return x;
    }
    public static Simplest[] Ones(int len)
    {
        Simplest[] x = new Simplest[len];
        for (int i = 0; i < len; i++)
        {
            x[i] = One();
        }
        return x;
    }
    public static Simplest[,] Zeros(int nRows, int nCols)
    {
        Simplest[,] x = new Simplest[nRows, nCols];
        for (int i = 0; i < nRows; i++)
        {
            for (int j = 0; j < nCols; j++)
            {
                x[i, j] = Zero();
            }
        }
        return x;
    }

    public static Simplest W()
    {
        return new Simplest(Rank.W_TO_THE_K, 1);
    }

    public static Simplest Finite(double k)
    {
        return new Simplest(Rank.FINITE, k);
    }

    public static Simplest Bottom(Rank rank)
    {
        switch (rank)
        {
            case Rank.FINITE:
                return Simplest.Zero();
            case Rank.W_TO_THE_K:
                return new Simplest(rank, 1);
            case Rank.TWO_TO_THE_W:
                return new Simplest(rank, -1);
            case Rank.STACK_W:
                return new Simplest(rank, 2);
            default:
                throw new Shared.ValueError();
        }
    }
    public void ToJSON(StreamWriter writer)
    {
        writer.WriteLine("[");
        writer.Write((int)MyRank);
        writer.WriteLine(',');
        writer.Write(K);
        writer.WriteLine(',');
        writer.WriteLine("],");
    }
    public static Simplest FromJSON(StreamReader reader)
    {
        Shared.Assert(reader.ReadLine().Equals("["));
        Rank rank = (Rank)Int32.Parse(JSON.NoLast(reader));
        double k = Double.Parse(JSON.NoLast(reader));
        Simplest s = new Simplest(rank, k);
        Shared.Assert(reader.ReadLine().Equals("],"));
        return s;
    }
}
