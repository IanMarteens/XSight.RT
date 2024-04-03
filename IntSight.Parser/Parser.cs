using static System.Runtime.CompilerServices.Unsafe;
using Rsc = IntSight.Parser.Properties.Resources;

namespace IntSight.Parser;

/// <summary>The LALR Engine.</summary>
/// <remarks>
/// Grammar Translator generates a new class derived from Parser.
/// You must derived another class from it, and implement the abstract methods
/// <seealso cref="Parser.Reduce">Reduce</seealso> and
/// <seealso cref="SyntaxError">SyntaxError</seealso>.
/// </remarks>
public abstract class Parser
{
    // Rule information
    private readonly int maxProdSize;
    private readonly ushort[] rules;
    // LALR tables
    private readonly int[] actoffs, gotoffs;
    private readonly ushort[] actions;
    // DFA tables
    private readonly ushort[] dfaStates, dfaCharsets, dfaFirst;
    private readonly int dfaFirstLength;
    private readonly bool[] dfaKeywords;
    // LALR engine
    protected ISource source;
    /// <summary>Errors discovered by the syntax analyzer.</summary>
    protected Errors errors;
    protected Token input, stack, freeList;
    /// <summary>Position of each item in the reduced rule's right side.</summary>
    protected SourceRange[] ranges;
    /// <summary>Position of text corresponding to the reduced rule.</summary>
    protected SourceRange ResultRange;
    // Constant values
    private const int MAX_ATTEMPTS = 3;
    // LALR actions
    private const int LALR_SHIFT = 0;
    private const int LALR_REDUCE = 1;
    private const int LALR_SINGLEREDUCE = 2;
    private const int LALR_REDUCE_NULL = 3;
    private const int LALR_REDUCE_0 = 4;
    private const int LALR_REDUCE_SANDWICH = 5;
    private const int LALR_ACCEPT = 6;
    //private const int LALR_REDUCE_1 = 7;
    // Special terminals
    private const ushort EOF_SYMBOL = 0;
    private const ushort ERROR_SYMBOL = 1;
    protected Dictionary<int, RecoveryAction> recoveryActions = [];
    // Protection against infinite loops in error recovery.
    private bool errorLoop;

    // Expected tokens for error handling
    protected List<int> ExpectedTokens { get; private set; } = [];

    /// <summary>
    /// Called after a new input token is pushed on the input stack.
    /// </summary>
    protected virtual void OnRetrieve() { }

    /// <summary>Called when a rule is reduced by the parser.</summary>
    /// <param name="rule">Rule number</param>
    /// <param name="e">Object array with semantic attributes.</param>
    /// <returns>A new synthesized attribute.</returns>
    protected abstract object OnReduce(int rule, object[] e);

    /// <summary>Check whether symbol is a terminal or a non terminal.</summary>
    /// <param name="symbol">The numeric value which identifies the symbol.</param>
    /// <returns>True, if symbol is a terminal symbol.</returns>
    protected abstract bool IsTerminal(int symbol);

    /// <summary>Called when parsing starts.</summary>
    protected virtual void InitializeParser() { }

    /// <summary>Called whenever an error is detected by the parser.</summary>
    /// <param name="retry">
    /// Holds false initially. You must change it to true if parsing should be resumed.
    /// </param>
    protected virtual void SyntaxError(ref bool retry, ref ushort state, int retryTimes)
    {
        int deltaStack = 0;
        bool inputMoved = false;
        while (stack != null)
        {
            if (recoveryActions.TryGetValue(stack.State, out RecoveryAction action))
            {
                inputMoved |= DiscardInput(action.NextTerminals);
                state = action.NewState;
                Push(state);
                deltaStack++;
                retry = true;
                if (!inputMoved && deltaStack >= 0)
                    // The input pointer has not moved,
                    // and there have been no changes in the stack size.
                    // We're allowed to do this only once.
                    if (errorLoop)
                        retry = false;
                    else
                        errorLoop = true;
                return;
            }
            else
            {
                stack = stack.Next;
                deltaStack--;
            }
        }
    }

    /// <summary>
    /// Constructs a parser instance with the specified DFA and LALR tables.
    /// </summary>
    /// <param name="eofSymbol">Terminal value for the end of file.</param>
    /// <param name="errorSymbol">Special terminal value for invalid tokens.</param>
    /// <param name="maxProdSize">Size of the longest right side in a rule.</param>
    /// <param name="rules">Rules table.</param>
    /// <param name="actoffs">LALR actions offsets.</param>
    /// <param name="gotoffs">LALR goto table offsets.</param>
    /// <param name="actions">Table containing both action & goto information.</param>
    /// <param name="dfaStates">DFA transitions.</param>
    /// <param name="dfaCharsets">DFA character sets definitions.</param>
    /// <param name="dfaFirst">The first DFA transition.</param>
    /// <param name="dfaKeywords">Tells which tokens are keywords.</param>
    /// <remarks>
    /// The gramtrans.exe tool generates a class called <c>BaseParser</c> inheriting
    /// from this class. <c>BaseParser</c> parameterless constructor takes care of
    /// providing all these table arrays to its ancestor.
    /// </remarks>
    protected Parser(
        ushort eofSymbol, ushort errorSymbol, int maxProdSize,
        ushort[] rules, int[] actoffs, int[] gotoffs, ushort[] actions,
        ushort[] dfaStates, ushort[] dfaCharsets, ushort[] dfaFirst,
        bool[] dfaKeywords)
    {
        Debug.Assert(eofSymbol == EOF_SYMBOL, "EOF symbol constant has changed");
        Debug.Assert(errorSymbol == ERROR_SYMBOL, "Error symbol constant has changed");

        this.maxProdSize = maxProdSize;
        // LALR tables
        this.rules = rules;
        this.actoffs = actoffs;
        this.gotoffs = gotoffs;
        this.actions = actions;
        // DFA tables
        this.dfaStates = dfaStates;
        this.dfaCharsets = dfaCharsets;
        this.dfaFirst = dfaFirst;
        dfaFirstLength = dfaFirst.Length;
        this.dfaKeywords = dfaKeywords;
        // LALR engine
        ranges = new SourceRange[maxProdSize];
    }

    /// <summary>Moves all tokens in the stack to the free nodes list.</summary>
    protected void ClearStack()
    {
        Token t = stack;
        if (t != null)
        {
            for (; t.Next != null; t = t.Next)
                t.Data = null;
            t.Data = null;
            t.Next = freeList;
            freeList = stack;
            stack = null;
        }
    }

    /// <summary>Puts a nonterminal symbol in the top of the parsing stack.</summary>
    /// <param name="state">The state index.</param>
    /// <remarks>This variant pushes a null data pointer.</remarks>
    private void Push(ushort state)
    {
        Token result;
        if (freeList == null)
            result = new();
        else
        {
            result = freeList;
            freeList = result.Next;
            result.Data = null;
        }
        result.Next = stack;
        result.State = state;
        result.Position = ResultRange;
        stack = result;
    }

    protected int CurrentNonTerminal(ushort state)
    {
        int rule = -1;
        for (int offset = actoffs[state]; actions[offset] != 0xffff; offset += 3)
            switch (actions[offset + 2])
            {
                case LALR_ACCEPT:
                case LALR_SHIFT:
                    return -1;
                default:
                    if (rule == -1)
                        rule = actions[offset + 1];
                    else if (rule != actions[offset + 1])
                        return -1;
                    break;
            }
        return rule == -1 || 2 * rule >= rules.Length ? -1 : rules[2 * rule];
    }

    /// <summary>
    /// Gathers aceptable terminals, given a LALR state, into <c>expectedTokens</c>.
    /// </summary>
    /// <param name="state">The LALR state being analyzed.</param>
    /// <remarks>
    /// When the expected tokens set is small enough, it can be useful for
    /// generating a sensible error message.
    /// </remarks>
    protected void FindExpectedTokens(int state)
    {
        ExpectedTokens.Clear();
        for (int offset = actoffs[state]; actions[offset] != 0xffff; offset += 3)
            ExpectedTokens.Add(actions[offset]);
    }

    /// <summary>Advances one token in the input document.</summary>
    /// <returns>The numeric identifier for the terminal symbol.</returns>
    /// <remarks>
    /// As a side effect, a token is assigned to <c>Parser.input</c>.
    /// </remarks>
    protected ushort Retrieve()
    {
        int position = 0, lastPos = 0, state;
        ushort character = source.FirstChar, lastAccept = ERROR_SYMBOL;
        if (character < dfaFirstLength && (state = dfaFirst[character]) != 0)
        {
            ref ushort dfaChar = ref dfaCharsets[0];
        TRY_STATE:
            // States layout: accept, cset, jmp, cset, jmp, ... , 0xffff
            ushort accept = dfaStates[state++];
            if (accept != 0xffff)
            {
                lastAccept = accept;
                lastPos = position;
            }
            character = source[++position];
        TRY_CHARSET:
            int i = dfaStates[state];
            if (i == 0xffff)
                goto QUIT;
            // Charsets layout: up1, lo1, ... , upN, loN, 0xffff, 0xffff
            while (character > Add(ref dfaChar, i))
                i += 2;
            if (character < Add(ref dfaChar, i + 1))
            {
                state += 2;
                goto TRY_CHARSET;
            }
            state = dfaStates[state + 1];
            goto TRY_STATE;
        }
    QUIT:
        lastPos++;
        if (freeList == null)
            input = new();
        else
        {
            input = freeList;
            freeList = input.Next;
            input.Next = null;
        }
        input.Data = dfaKeywords[lastAccept] ? source.Skip(lastPos) : source.Read(lastPos);
        input.State = lastAccept;
        input.Position = source.GetRange(lastPos);
        OnRetrieve();
        return input.State;
    }

    /// <summary>Parses an input file and returns an AST.</summary>
    /// <param name="source">The input stream.</param>
    /// <param name="errors">Any errors will be reported here.</param>
    /// <param name="data">The Abstract Syntax Tree.</param>
    /// <returns>True if succeeded, false if any error was found.</returns>
    protected bool Execute<TData>(ISource source, Errors errors, out TData data)
        where TData : class
    {
        data = null;
        this.source = source;
        this.errors = errors;
        input = null;
        errorLoop = false;
        ClearStack();
        InitializeParser();
        ushort symbol, state = 0;
        ResultRange = new(source.Document, 1);
        Push(0);
        int retry, idx;
        object topData;
        object[] tokens = new object[maxProdSize];
        ref ushort act = ref actions[0];
        SourceRange[] ranges = this.ranges;
    START_N_READ:
        if (Retrieve() == ERROR_SYMBOL)
        {
            errors.Add(source.GetRange(input.Text.Length),
                Rsc.InvalidCharacter, input.Text);
            ClearStack();
            return false;
        }
        symbol = input.State;
    NEXT:
        retry = 0;
    RETRY:
        // Actions layout: symbol, state, value, ... , 0xffff
        idx = actoffs[state];
        while (Add(ref act, idx) < symbol)
            idx += 3;
        if (Add(ref act, idx) != symbol)
        {
            bool canRetry = false;
            int errcount = errors.Count;
            SyntaxError(ref canRetry, ref state, retry);
            if (canRetry && input != null && retry < MAX_ATTEMPTS)
            {
                retry++;
                symbol = input.State;
                goto RETRY;
            }
            if (errcount == errors.Count)
                errors.Add(input.Position, Rsc.SyntaxError);
            ClearStack();
            return false;
        }
        switch (Add(ref act, idx + 2))
        {
            case LALR_SHIFT:
                input.State = state = Add(ref act, idx + 1);
                input.Next = stack;
                stack = input;
                goto START_N_READ;
            case LALR_REDUCE:
                // Get the rule number.
                retry = Add(ref act, idx + 1);
                // Rules layout: head, size (two items by rule).
                idx = retry + retry;
                state = rules[idx];
                ushort ruleSize = rules[idx + 1];
                idx = ruleSize;
                do
                {
                    Token t = stack;
                    tokens[--idx] = t.Data;
                    ranges[idx] = t.Position;
                    stack = t.Next;
                    t.Next = freeList;
                    freeList = t;
                }
                while (idx > 0);
                ResultRange = new(ranges[0], ranges[ruleSize - 1]);
                // Gotos layout: symbol, state, ...
                idx = gotoffs[stack.State];
                while (Add(ref act, idx) < state)
                    idx += 2;
                state = Add(ref act, idx + 1);
                topData = OnReduce(retry, tokens);
                {
                    Token t = freeList;
                    freeList = t.Next;
                    t.Next = stack;
                    t.State = state;
                    t.Data = topData;
                    t.Position = ResultRange;
                    stack = t;
                }
                if (topData is IAstNode topNode)
                    topNode.Position = ResultRange;
                goto NEXT;
            case LALR_SINGLEREDUCE:
                // Single reductions directly encodes the rule head with the action.
                state = Add(ref act, idx + 1);
                // Gotos layout: symbol, state, ... , 0xffff
                idx = gotoffs[stack.Next.State];
                while (Add(ref act, idx) < state)
                    idx += 2;
                stack.State = state = Add(ref act, idx + 1);
                goto NEXT;
            case LALR_REDUCE_NULL:
                // Null reductions directly encodes the rule head with the action.
                state = Add(ref act, idx + 1);
                ResultRange = input.Position.FromStart();
                // Gotos layout: symbol, state, ... , 0xffff
                idx = gotoffs[stack.State];
                while (Add(ref act, idx) < state)
                    idx += 2;
                Push(state = Add(ref act, idx + 1));
                goto NEXT;
            case LALR_REDUCE_0:
                ResultRange = input.Position.FromStart();
                // Get the rule number.
                retry = Add(ref act, idx + 1);
                // Rules layout: head, size, ...
                state = rules[retry + retry];
                // Gotos layout: symbol, state, ... , 0xffff
                idx = gotoffs[stack.State];
                while (Add(ref act, idx) < state)
                    idx += 2;
                Push(state = Add(ref act, idx + 1));
                stack.Data = OnReduce(retry, null);
                // By default, position is not propagated to the semantic result.
                goto NEXT;
            case LALR_REDUCE_SANDWICH:
                // This index has already been multiplied by two.
                // We do not need the rule number at all, in this case!
                idx = Add(ref act, idx + 1);
                // Rules layout: head, size, ...
                {
                    Token t = stack;
                    ResultRange = t.Position;
                    if (rules[idx + 1] == 3)
                    {
                        stack = t.Next;
                        t.Next = freeList;
                        freeList = t;
                        t = stack;
                    }
                    topData = t.Data;
                    stack = t.Next;
                    t.Next = freeList;
                    freeList = t;
                }
                // Gotos layout: symbol, state, ...
                state = rules[idx];
                idx = gotoffs[stack.Next.State];
                while (Add(ref act, idx) < state)
                    idx += 2;
                stack.State = state = Add(ref act, idx + 1);
                stack.Position = ResultRange = new(stack.Position, ResultRange);
                stack.Data = topData;
                topNode = topData as IAstNode;
                if (topNode != null)
                    topNode.Position = ResultRange;
                goto NEXT;
            case LALR_ACCEPT:
                data = stack.Data as TData;
                return true;
            default:
                // This is a LALR_REDUCE_1 action.
                ResultRange = stack.Position;
                tokens[0] = stack.Data;
                ranges[0] = ResultRange;
                // The nonterminal has been coded in the Action field.
                state = Add(ref act, idx + 2);
                // We won't touch "idx", since we'll need it later.
                // So, we reuse "retryTimes" to scan the GOTO table.
                // Gotos layout: symbol, state, ... , 0xffff
                retry = gotoffs[stack.Next.State];
                while (Add(ref act, retry) < state)
                    retry += 2;
                stack.State = state = Add(ref act, retry + 1);
                // Now we use "idx" to find the rule number.
                stack.Data = topData = OnReduce(Add(ref act, idx + 1), tokens);
                topNode = topData as IAstNode;
                if (topNode != null)
                    topNode.Position = ResultRange;
                goto NEXT;
        }
    }

    /// <summary>Discard items until end of file or a stop symbol is found.</summary>
    /// <param name="stopSymbols">Sorted list of stop symbols.</param>
    /// <returns>True if the input has moved; false, otherwise.</returns>
    protected bool DiscardInput(params int[] stopSymbols)
    {
        bool inputMoved = false;
        while (true)
            if (input == null)
            {
                Retrieve();
                inputMoved = true;
            }
            else if (input.State == EOF_SYMBOL)
                return inputMoved;
            else
            {
                int lo = 0, hi = stopSymbols.Length - 1;
                do
                {
                    int mid = (lo + hi) / 2;
                    int s = stopSymbols[mid];
                    if (input.State == s)
                        return inputMoved;
                    if (input.State < s)
                        hi = mid - 1;
                    else
                        lo = mid + 1;
                }
                while (lo <= hi);
                // Input is not a stop symbol: discard token.
                input = null;
                Retrieve();
                inputMoved = true;
            }
    }

    /// <summary>
    /// Register important non terminals and their NEXT terminals subsets
    /// in order to create a recovery table.
    /// </summary>
    /// <param name="nonterminals">A list of significative non terminals.</param>
    /// <param name="nextSets">A list of (sorted) terminal sets.</param>
    protected void CreateRecoveryTable(int[] nonterminals, int[][] nextSets)
    {
        Debug.Assert(
            nonterminals.Length == nextSets.Length,
            "CreateRecoveryTable: mismatch between nonterminals and NEXT subsets.");
        // Iterate over LALR states.
        for (int state = 0; state < gotoffs.Length; state++)
        {
            int gotoOffset = gotoffs[state];
            if (gotoOffset >= 0)
            {
                // Find how many goto entries this state has.
                int nextState = state + 1;
                int stopAt = actions.Length - 1;
                while (nextState < gotoffs.Length && gotoffs[nextState] <= gotoOffset)
                    nextState++;
                if (nextState < gotoffs.Length)
                    stopAt = gotoffs[nextState];
                // Iterate over the goto table.
                Debug.Assert(
                    gotoOffset < stopAt,
                    "CreateRecoveryTable: Corrupted GOTO table");
                while (gotoOffset < stopAt)
                {
                    int idx = Array.IndexOf(nonterminals, actions[gotoOffset]);
                    if (idx == -1)
                        gotoOffset++;
                    else
                    {
                        recoveryActions.Add(
                            state, new(actions[gotoOffset + 1], nextSets[idx]));
                        break;
                    }
                }
            }
        }
    }

    /// <summary>Encodes a stack recovery action.</summary>
    protected class RecoveryAction
    {
        public readonly ushort NewState;
        public readonly int[] NextTerminals;

        public RecoveryAction(ushort newState, int[] nextTerminals) =>
            (NewState, NextTerminals) = (newState, nextTerminals);
    }

    /// <summary>
    /// Each instance contain information about a symbol (may be terminal or non terminal),
    /// a LALR state, a position in the input stream, and semantic data.
    /// </summary>
    protected sealed class Token
    {
        /// <summary>Semantic attribute attached to this symbol.</summary>
        /// <remarks>For terminals, this is the associated text.</remarks>
        public object Data;
        /// <summary>Position of this symbol inside the input stream.</summary>
        public SourceRange Position;
        /// <summary>Holds the LALR state for this symbol.</summary>
        public ushort State;
        /// <summary>Semantic attribute represented as a string.</summary>
        public string Text => Data != null ? Data.ToString() : string.Empty;
        /// <summary>Next token in the LALR stack.</summary>
        public Token Next;
    }
}
