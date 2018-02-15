﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Crevice.Core.FSM
{
    using System.Linq;
    using Crevice.Core.Events;
    using Crevice.Core.Context;
    using Crevice.Core.DSL;

    public abstract class State<TConfig, TContextManager, TEvalContext, TExecContext>
        where TConfig : GestureMachineConfig
        where TContextManager : ContextManager<TEvalContext, TExecContext>
        where TEvalContext : EvaluationContext
        where TExecContext : ExecutionContext
    {
        public int Depth { get; }

        public State(int depth)
        {
            Depth = depth;
        }

        public virtual Result<TConfig, TContextManager, TEvalContext, TExecContext> Input(IPhysicalEvent evnt)
        {
            return Result.Create(eventIsConsumed: false, nextState: this);
        }

        public virtual State<TConfig, TContextManager, TEvalContext, TExecContext> Timeout()
            => this;

        public virtual State<TConfig, TContextManager, TEvalContext, TExecContext> Reset()
            => this;

        protected static bool CanTransition(
            IReadOnlyList<IReadOnlyDoubleThrowElement<TExecContext>> doubleThrowElements)
            => doubleThrowElements.Any(d =>
                    d.DoExecutors.Any() ||
                    d.StrokeElements.Any(ds => ds.IsFull) ||
                    d.SingleThrowElements.Any(ds => ds.IsFull) ||
                    d.DoubleThrowElements.Any(dd => dd.IsFull));

        protected static bool HasPressExecutors(
            IReadOnlyList<IReadOnlyDoubleThrowElement<TExecContext>> doubleThrowElements)
            => doubleThrowElements.Any(d => d.PressExecutors.Any());

        protected static bool HasDoExecutors(
            IReadOnlyList<IReadOnlyDoubleThrowElement<TExecContext>> doubleThrowElements)
            => doubleThrowElements.Any(d => d.DoExecutors.Any());

        protected static bool HasReleaseExecutors(
            IReadOnlyList<IReadOnlyDoubleThrowElement<TExecContext>> doubleThrowElements)
            => doubleThrowElements.Any(d => d.ReleaseExecutors.Any());

        public bool IsState0 => GetType() == typeof(State0<TConfig, TContextManager, TEvalContext, TExecContext>);
        public bool IsStateN => GetType() == typeof(StateN<TConfig, TContextManager, TEvalContext, TExecContext>);

        public State0<TConfig, TContextManager, TEvalContext, TExecContext> ToState0()
            => this as State0<TConfig, TContextManager, TEvalContext, TExecContext>;

        public StateN<TConfig, TContextManager, TEvalContext, TExecContext> ToStateN()
            => this as StateN<TConfig, TContextManager, TEvalContext, TExecContext>;
    }
}
