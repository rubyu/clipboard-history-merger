﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreviceApp.Core.FSM
{
    #region State Definition
    // S0
    //
    // Inital state.

    // S1
    //
    // The state holding primary double action mouse button.

    // S2
    //
    // The state holding primary and secondary double action mouse buttons.
    #endregion
        
    using WinAPI.WindowsHookEx;
    
    public interface IState
    {
        Result Input(Def.Event.IEvent evnt, LowLevelMouseHook.POINT point);
        IState Reset();
    }
    
    public abstract class State : IState
    {
        internal readonly GlobalValues Global;

        public State(GlobalValues Global)
        {
            this.Global = Global;
        }

        // Check whether given event must be ignored or not.
        // Return true if given event is in the ignore list, and remove it from ignore list.
        // Return false if the pair of given event is in the ignore list, and remove it from ignore list.
        // Otherwise return false.
        public bool MustBeIgnored(Def.Event.IEvent evnt)
        {
            if (evnt is Def.Event.IDoubleActionRelease)
            {
                var ev = evnt as Def.Event.IDoubleActionRelease;
                if (Global.IgnoreNext.Contains(ev))
                {
                    Global.IgnoreNext.Remove(ev);
                    return true;
                }
            }
            else if (evnt is Def.Event.IDoubleActionSet)
            {
                var ev = evnt as Def.Event.IDoubleActionSet;
                var p = ev.GetPair();
                if (Global.IgnoreNext.Contains(p))
                {
                    Global.IgnoreNext.Remove(p);
                    return false;
                }
            }
            return false;
        }

        public virtual Result Input(Def.Event.IEvent evnt, LowLevelMouseHook.POINT point)
        {
            return Result.EventIsRemaining(nextState: this);
        }

        public virtual IState Reset()
        {
            throw new InvalidOperationException();
        }

        public static void ExecuteSafely(UserActionExecutionContext ctx, DSL.Def.DoFunc func)
        {
            try
            {
                func(ctx);
            }
            catch (Exception ex)
            {
                Debug.Print("{0} occured when executing a DoFunc of a gesture. This error may automatically be recovered.", 
                    ex.GetType().Name);
            }
        }

        public static bool EvaluateSafely(UserActionExecutionContext ctx, DSL.Def.WhenFunc func)
        {
            try
            {
                return func(ctx);
            }
            catch (Exception ex)
            {
                Debug.Print("{0} occured when evaluating a WhenFunc of a gesture. This error may automatically be recovered.",
                    ex.GetType().Name);
            }
            return false;
        }

        public void IgnoreNext(Def.Event.IDoubleActionRelease evnt)
        {
            Debug.Print("{0} added to global ignore list. The `release` event of it will be ignored next time.", evnt.GetType().Name);
            Global.IgnoreNext.Add(evnt);
        }
    }
            
}