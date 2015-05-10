﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.JScript;
using Microsoft.JScript.Vsa;

namespace FileParsing
{
    class Condition
    {
        private static readonly string[] AllowedCondCharacters = {"<", ">", "==", "!="};
        private string data;

        public static int GetEndPositionOfBracket(string str)
        {
            int currentPos = str.IndexOf('(') + 1;
            int countNotClosedBrackets = 1;
            while (countNotClosedBrackets != 0)
            {
                if (str[currentPos] == ')')
                {
                    countNotClosedBrackets -= 1;
                }
                else if (str[currentPos] == '(')
                {
                    countNotClosedBrackets += 1;
                }
                currentPos += 1;
            }
            return currentPos - 1;
        }

        public Condition(string cond)
        {
            data = cond;
        }

        private void EmitRedundantCharacters()
        {
            data = new string(data.Reverse().ToArray());
            data = new string((data.SkipWhile(c =>
            { return StaticData.CharactersToIgnore.Any(character => c == character); })).ToArray());
            data = new string(data.Reverse().ToArray());
            data = new string((data.SkipWhile(c =>
            { return StaticData.CharactersToIgnore.Any(character => c == character); })).ToArray());
        }

        private object ParseArgument(Context context, string arg)
        {
            object res;
            if (arg.Contains(StaticData.VariableSeparator) && context.Contains(arg.Remove(0, 1)))
            {
                res = context.GetValue(arg.Remove(0, 1));
            }
            else if (arg.Contains('\"'))
            {
                if (arg.IndexOf('\"') == 0 && arg.LastIndexOf('\"') == arg.Length - 1)
                {
                    res = arg.Remove(0,1).Remove(arg.Length - 1 - 1, 1);
                }
                else
                {
                    throw new ArgumentException("Invalid format of string!");
                }
            }
            else if (arg.Contains('.'))
            {
                res = double.Parse(arg);
            }
            else
            {
                res = int.Parse(arg);
            }
            return res;
        }
        private void FillArgValues(Context context, string ch, out object firstArgRes, out object secondArgRes)
        {
            EmitRedundantCharacters();
            string firstArg = data.Substring(0,
                data.IndexOfAny(StaticData.CharactersToIgnore.Concat(new[] {ch[0]}).ToArray()));   //(new[] { ' ', '\n', '\r', '\t', ch[0] }));
            string secondArg =
                data.Substring(
                    data.LastIndexOfAny(StaticData.CharactersToIgnore.Concat(new[] {ch[ch.Length - 1]}).ToArray()) + 1);  //(new[] { ' ', '\n', '\r', '\t', ch[ch.Length - 1] })+1);
            firstArgRes = ParseArgument(context, firstArg);
            secondArgRes = ParseArgument(context, secondArg);
        }
        private bool EvaluateConditionWithCharacter(Context context, string ch)
        {
            object firstArgRes;
            object secondArgRes;
            FillArgValues(context, ch, out firstArgRes, out secondArgRes);
            switch (ch)
            {
                case "==":
                    return firstArgRes.ToString() == secondArgRes.ToString();
                case "!=":
                    return firstArgRes.ToString() != secondArgRes.ToString();
                case "<":
                    if (firstArgRes is double && secondArgRes is double)
                    {
                        return ((double) firstArgRes).CompareTo((double) secondArgRes) < 0;
                    }
                    return ((int)firstArgRes).CompareTo((int)secondArgRes) < 0;
                case ">":
                    if (firstArgRes is double && secondArgRes is double)
                    {
                        return ((double) firstArgRes).CompareTo((double) secondArgRes) > 0;
                    }
                    return ((int)firstArgRes).CompareTo((int)secondArgRes) > 0;
            }
            return false;
        }

        private bool EvaluateConditionWithoutCharacters(Context context)
        {
            EmitRedundantCharacters();
            return ParseArgument(context, data) != null;
        }
        public bool Evaluate(Context context)
        {
            StringBuilder builder = new StringBuilder(data);
            List<string> valuesToReplace = new List<string>();
            int startingPosition = 0;
            int positionOfVariable;
            while ((positionOfVariable = data.IndexOf(StaticData.VariableSeparator, startingPosition)) != -1)
            {
                startingPosition = StaticText.GetEndOfVariableAtPosition(data, positionOfVariable + 1);
                valuesToReplace.Add(data.Substring(positionOfVariable + 1, startingPosition - positionOfVariable));
            }
            foreach (var val in valuesToReplace.Where(context.Contains))
            {
                string replaceTo;
                object value = context.GetValue(val);
                double res;
                if (double.TryParse(value.ToString(), out res))
                {
                    replaceTo = res.ToString();
                }
                else
                {
                    replaceTo = '"' + value.ToString() + '"';
                }
                builder.Replace(StaticData.VariableSeparator + val, replaceTo);
            }
            string condition = builder.ToString();
            var engine = VsaEngine.CreateEngine();
            var result = Eval.JScriptEvaluate(condition, engine);
            return bool.Parse(result.ToString());
        }

        //public bool Evaluate(Context context)
        //{
        //    foreach (string allowed in AllowedCondCharacters)
        //    {
        //        if (data.Contains(allowed))
        //        {
        //            return EvaluateConditionWithCharacter(context, allowed);
        //        }
        //    }
        //    return EvaluateConditionWithoutCharacters(context);
        //}
    }
}