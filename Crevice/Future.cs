﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crevice.Future
{
    using System.Drawing;

    // interface ISetupable


    // EvaluationContextはVoid -> EvaluationContextはVoidな任意のGeneratorを渡して生成できる？
    // ExecutionContextは同様にEvaluationContext -> ExecutionContextで？

    // todo
    public class EvaluationContext { }
    public class ExecutionContext { }

    public delegate bool EvaluateAction<in T>(T ctx);
    public delegate void ExecuteAction<in T>(T ctx);

    public class Result
    {
        public class EventResult
        {
            public readonly bool IsConsumed;
            public EventResult(bool consumed)
            {
                IsConsumed = consumed;
            }
        }

        public EventResult Event;
        public IState NextState { get; private set; }

        private Result(bool consumed, IState nextState)
        {
            this.Event = new EventResult(consumed);
            this.NextState = nextState;
        }

        public static Result EventIsConsumed(IState nextState)
        {
            return new Result(true, nextState);
        }

        public static Result EventIsRemained(IState nextState)
        {
            return new Result(false, nextState);
        }
    }

    public abstract class GestureMachine<TEvalContext, TExecContext>
        where TEvalContext : EvaluationContext
        where TExecContext : ExecutionContext
    {
        public IReadOnlyList<StrokeEvent.Direction> GetStroke()
        {
            throw new NotImplementedException();
        }

        public virtual TEvalContext CreateEvaluateContext()
        {
            throw new NotImplementedException();
        }

        public virtual TExecContext CreateExecutionContext(TEvalContext evaluationContext)
        {

            throw new NotImplementedException();
        }


        // inputを physicalな型にするとよい？
        // HashSetでは微妙かな やはりカウンターがよさげ
        private readonly HashSet<IReleaseEvent> invalidReleaseEvents = new HashSet<IReleaseEvent>();

        public bool IsIgnored(IReleaseEvent releaseEvent)
        {
            if (invalidReleaseEvents.Contains(releaseEvent))
            {
                invalidReleaseEvents.Remove(releaseEvent);
                return true;
            }
            return false;
        }
        
        public void IgnoreNext(IReleaseEvent releaseEvent)
        {
            if (!invalidReleaseEvents.Add(releaseEvent))
            {
                throw new InvalidOperationException();
            }
        }

        public void IgnoreNext(IEnumerable<IReleaseEvent> releaseEvents)
        {
            foreach (var releaseEvent in releaseEvents)
            {
                IgnoreNext(releaseEvent);
            }
        }
    }

    public interface IState
    {
        Result Input(IPhysicalEvent evnt);
    }


    // todo type
    public abstract class State<TEvalContext, TExecContext> : IState
        where TEvalContext : EvaluationContext
        where TExecContext : ExecutionContext
    {
        // global variable
        public GestureMachine<TEvalContext, TExecContext> Machine;

        public virtual Result Input(IPhysicalEvent evnt)
        {
            return Result.EventIsRemained(nextState: this);
        }
    }
    
    public class State0<TEvalContext, TExecContext> : State<TEvalContext, TExecContext>
        where TEvalContext : EvaluationContext
        where TExecContext : ExecutionContext
    {
        public readonly RootElement<TEvalContext, TExecContext> RootElement;

        public State0(
            GestureMachine<TEvalContext, TExecContext> gestureMachine,
            RootElement<TEvalContext, TExecContext> rootElement)
        {
            Machine = gestureMachine;
            RootElement = rootElement; 
        }

        public override Result Input(IPhysicalEvent evnt)
        {
            if (evnt is IFireEvent fireEvent && 
                   (SingleThrowTriggers.Contains(fireEvent) || 
                    SingleThrowTriggers.Contains(fireEvent.LogicalNormalized)))
            {
                var evalContext = Machine.CreateEvaluateContext();

                var singleThrowElements = GetActiveSingleThrowElements(evalContext, fireEvent);
                if (singleThrowElements.Count > 0)
                {
                    var execContext = Machine.CreateExecutionContext(evalContext);
                    foreach (var st in singleThrowElements)
                    {
                        foreach (var doExecutor in st.DoExecutors)
                        {
                            doExecutor(execContext);
                        }
                    }
                    return Result.EventIsConsumed(nextState: this);
                }
            }
            else if (evnt is IPressEvent pressEvent && 
                        (DoubleThrowTriggers.Contains(pressEvent) ||
                         DoubleThrowTriggers.Contains(pressEvent.LogicalNormalized)))
            {
                var evalContext = Machine.CreateEvaluateContext();
                var doubleThrowElements = GetActiveDoubleThrowElements(evalContext, pressEvent);
                if (doubleThrowElements.Count() > 0)
                {
                    var execContext = Machine.CreateExecutionContext(evalContext);
                    foreach (var dt in doubleThrowElements)
                    {
                        foreach (var pressExecutor in dt.PressExecutors)
                        {
                            pressExecutor(execContext);
                        }
                    }
                    var state = new StateN<TEvalContext, TExecContext>(
                        evalContext,
                        CreateHistory(pressEvent, this),
                        doubleThrowElements,
                        allowCancel: true
                        );

                    return Result.EventIsConsumed(nextState: state);
                }
            }
            // これはMachineが担当したほうがよさげ
            else if (evnt is IReleaseEvent releaseEvent)
            {
                if (Machine.IsIgnored(releaseEvent))
                {
                    return Result.EventIsConsumed(nextState: this);
                }
            }
            
            return base.Input(evnt);
        }

        public IReadOnlyList<(IReleaseEvent, IState)> CreateHistory(IPressEvent pressEvent, IState state)
        {
            return new List<(IReleaseEvent, IState)>() {
                (pressEvent.Opposition, state)
            };
        }

        // Filter
        public IReadOnlyList<DoubleThrowElement<TExecContext>> GetActiveDoubleThrowElements(TEvalContext ctx, IPressEvent triggerEvent)
        {
            return (
                from w in RootElement.WhenElements
                where w.IsFull &&
                      w.WhenEvaluator(ctx)
                select (
                    from d in w.DoubleThrowElements
                    where d.IsFull && (d.Trigger == triggerEvent || 
                                       d.Trigger == triggerEvent.LogicalNormalized)
                    select d))
                .Aggregate(new List<DoubleThrowElement<TExecContext>>(), (a, b) => { a.AddRange(b); return a; });
        }

        public IReadOnlyList<SingleThrowElement<TExecContext>> GetActiveSingleThrowElements(TEvalContext ctx, IFireEvent triggerEvent)
        {
            return (
                from w in RootElement.WhenElements
                where w.IsFull && 
                      w.WhenEvaluator(ctx)
                select (
                    from s in w.SingleThrowElements
                    where s.IsFull && (s.Trigger == triggerEvent ||
                                       s.Trigger == triggerEvent.LogicalNormalized)
                    select s))
                .Aggregate(new List<SingleThrowElement<TExecContext>>(), (a, b) => { a.AddRange(b); return a; } );
        }

        public IReadOnlyCollection<IFireEvent> SingleThrowTriggers
        {
            get
            {
                return (
                    from w in RootElement.WhenElements
                    where w.IsFull
                    select (
                        from s in w.SingleThrowElements
                        where s.IsFull
                        select s.Trigger))
                    .Aggregate(new HashSet<IFireEvent>(), (a, b) => { a.UnionWith(b); return a; });
            }
        }

        public IReadOnlyCollection<IPressEvent> DoubleThrowTriggers
        {
            get
            {
                return (
                    from w in RootElement.WhenElements
                    where w.IsFull
                    select (
                        from d in w.DoubleThrowElements
                        where d.IsFull
                        select d.Trigger))
                    .Aggregate(new HashSet<IPressEvent>(), (a, b) => { a.UnionWith(b); return a; });
            }
        }
    }

    /*
     * InputにはPhysicalなキーだけが来て、
     * HistoryにPhysicalなキーだけを残すなら、
     * Containsのところをフィルタリング結果>=0で判断できるし、
     * Releaseのところも特に問題ないのでは
     */
    
    public class StateN<TEvalContext, TExecContext> : State<TEvalContext, TExecContext>
        where TEvalContext : EvaluationContext
        where TExecContext : ExecutionContext
    {
        public readonly TEvalContext EvaluationContext;
        public readonly IReadOnlyList<(IReleaseEvent, IState)> History;
        public readonly IReadOnlyList<DoubleThrowElement<TExecContext>> DoubleThrowElements;
        public readonly bool CancelAllowed;

        public StateN(
            TEvalContext ctx,
            IReadOnlyList<(IReleaseEvent, IState)> history,
            IReadOnlyList<DoubleThrowElement<TExecContext>> doubleThrowElements,
            bool allowCancel = true
            )
        {
            EvaluationContext = ctx;
            History = history;
            DoubleThrowElements = doubleThrowElements;
            CancelAllowed = allowCancel;
        }

        public override Result Input(IPhysicalEvent evnt)
        {
            // Todo: storkewatcher

            if (evnt is IFireEvent fireEvent)
            {
                var singleThrowElements = GetSingleThrowElements(fireEvent);
                if (singleThrowElements.Count > 0)
                {
                    var execContext = Machine.CreateExecutionContext(EvaluationContext);
                    foreach (var st in singleThrowElements)
                    {
                        foreach (var doExecutor in st.DoExecutors)
                        {
                            doExecutor(execContext);
                        }
                    }
                    var notCancellableCopyState = new StateN<TEvalContext, TExecContext>(
                        EvaluationContext,
                        History,
                        DoubleThrowElements,
                        allowCancel: false);
                    return Result.EventIsConsumed(nextState: notCancellableCopyState);
                }
            }
            else if (evnt is IPressEvent pressEvent)
            {
                var doubleThrowElements = GetDoubleThrowElements(pressEvent);
                if (doubleThrowElements.Count > 0)
                {
                    var execContext = Machine.CreateExecutionContext(EvaluationContext);
                    foreach (var dt in doubleThrowElements)
                    {
                        foreach (var pressExecutor in dt.PressExecutors)
                        {
                            pressExecutor(execContext);
                        }
                    }
                    var nextState = new StateN<TEvalContext, TExecContext>(
                        EvaluationContext,
                        CreateHistory(History, pressEvent, this),
                        DoubleThrowElements,
                        allowCancel: true);
                    return Result.EventIsConsumed(nextState: nextState);
                }
            }
            else if (evnt is IReleaseEvent releaseEvent)
            {
                if (Machine.IsIgnored(releaseEvent))
                    // Machineにやらせたほうがよさげ
                {
                    return Result.EventIsConsumed(nextState: this);
                }
                else if (IsNormalEndTrigger(releaseEvent))
                {
                    var strokes = Machine.GetStroke();
                    if (strokes.Count() > 0)
                    {
                        var execContext = Machine.CreateExecutionContext(EvaluationContext);
                        // if match
                        foreach (var doubleThrowElements in DoubleThrowElements)
                        {
                            foreach (var doExecutor in doubleThrowElements.DoExecutors)
                            {
                                doExecutor(execContext);
                            }
                            foreach (var releaseExecutor in doubleThrowElements.ReleaseExecutors)
                            {
                                releaseExecutor(execContext);
                            }
                        }
                    }
                    else if (ShouldFinalize)
                    {
                        var execContext = Machine.CreateExecutionContext(EvaluationContext);
                        //normal end
                        foreach (var doubleThrowElements in DoubleThrowElements)
                        {
                            foreach (var doExecutor in doubleThrowElements.DoExecutors)
                            {
                                doExecutor(execContext);
                            }
                            foreach (var releaseExecutor in doubleThrowElements.ReleaseExecutors)
                            {
                                releaseExecutor(execContext);
                            }
                        }
                    }
                    else if (CancelAllowed)
                    {
                        // Machine.OnGestureCancel()

                        //何のインスタンスが来るかによって対応を変える必要がある
                        //例えばゲームパッドであれば何もする必要がない
                        return Result.EventIsConsumed(nextState: LastState);
                    }
                    return Result.EventIsConsumed(nextState: LastState);
                }
                else if (AbnormalEndTriggers.Contains(releaseEvent))
                {
                    var (oldState, skippedReleaseEvents) = FindStateFromHistory(releaseEvent);
                    Machine.IgnoreNext(skippedReleaseEvents);
                    return Result.EventIsConsumed(nextState: oldState);
                }
            }

            return base.Input(evnt);
        }

        public IState RequestTimeout()
        {
            if (CancelAllowed && !ShouldFinalize)
            {
                // Machine.OnGestureTimeout()

                return LastState;
            }
            return this;
        }


        public IState RequestCancel()
        {
            // Machine.OnGestureCancel()
            Machine.IgnoreNext(NormalEndTrigger);
            // Do Release 

            return LastState;
        }


        public IState LastState
        {
            get { return History.Last().Item2; }
        }
        
        public bool ShouldFinalize
        {
            get
            {
                return DoubleThrowElements.All(d =>
                        d.DoExecutors.Count == 0 &&
                        d.PressExecutors.Count == 0);
            }
        }

        public IReadOnlyList<(IReleaseEvent, IState)> CreateHistory(
            IReadOnlyList<(IReleaseEvent, IState)> history, 
            IPressEvent pressEvent,
            IState state)
        {
            var newHistory = history.ToList();
            newHistory.Add((pressEvent.Opposition, state));
            return newHistory;
        }

        public IReleaseEvent NormalEndTrigger
        {
            get { return History.Last().Item1; }
        }

        public bool IsNormalEndTrigger(IReleaseEvent releaseEvent)
        {
            return releaseEvent == NormalEndTrigger;
        }


        public IReadOnlyCollection<IReleaseEvent> AbnormalEndTriggers
        {
            get
            {
                return new HashSet<IReleaseEvent>(
                    from h in History.Reverse().Skip(1)
                    select h.Item1);
            }
        }

        public (IState, IReadOnlyList<IReleaseEvent>) FindStateFromHistory(IReleaseEvent releaseEvent)
        {

            var nextHistory = History.TakeWhile(t => t.Item1 != releaseEvent);
            var nextState = History[nextHistory.Count()].Item2;
            var skippedReleaseEvents = History.Skip(nextHistory.Count()).Select(t => t.Item1).ToList();
            return (nextState, skippedReleaseEvents);
        }


        public IReadOnlyList<DoubleThrowElement<TExecContext>> GetDoubleThrowElements(IPressEvent triggerEvent)
        {
            return (
                from d in DoubleThrowElements
                where d.IsFull
                select (
                    from dd in d.DoubleThrowElements
                    where dd.IsFull && (dd.Trigger == triggerEvent ||
                                        dd.Trigger == triggerEvent.LogicalNormalized)
                    select dd))
                .Aggregate(new List<DoubleThrowElement<TExecContext>>(), (a, b) => { a.AddRange(b); return a; });
        }

        public IReadOnlyList<SingleThrowElement<TExecContext>> GetSingleThrowElements(IFireEvent triggerEvent)
        {
            return (
                from d in DoubleThrowElements
                where d.IsFull
                select (
                    from s in d.SingleThrowElements
                    where s.IsFull && (s.Trigger == triggerEvent ||
                                       s.Trigger == triggerEvent.LogicalNormalized)
                    select s))
                .Aggregate(new List<SingleThrowElement<TExecContext>>(), (a, b) => { a.AddRange(b); return a; });
        }

        public IEnumerable<IFireEvent> SingleThrowTriggers
        {
            get
            {
                return (
                    from d in DoubleThrowElements
                    where d.IsFull
                    select (
                        from s in d.SingleThrowElements
                        where s.IsFull
                        select s.Trigger))
                    .Aggregate(new HashSet<IFireEvent>(), (a, b) => { a.UnionWith(b); return a; });
            }
        }

        public IEnumerable<IPressEvent> DoubleThrowTriggers
        {
            get
            {
                return (
                    from d in DoubleThrowElements
                    where d.IsFull
                    select (
                        from dd in d.DoubleThrowElements
                        where dd.IsFull
                        select dd.Trigger))
                    .Aggregate(new HashSet<IPressEvent>(), (a, b) => { a.UnionWith(b); return a; });
            }
        }

    }

    public abstract class Element
    {
        public abstract bool IsFull { get; }
    }

    /* RootElement
     * 
     * .When() -> new WhenElement
     */
    public class RootElement<TEvalContext, TExecContext> : Element
        where TEvalContext : EvaluationContext
        where TExecContext : ExecutionContext
    {
        public override bool IsFull
        {
            get => WhenElements.Any(e => e.IsFull);
        }

        private List<WhenElement<TEvalContext, TExecContext>> whenElements = new List<WhenElement<TEvalContext, TExecContext>>();

        public IReadOnlyCollection<WhenElement<TEvalContext, TExecContext>> WhenElements
        {
            get { return whenElements.ToList(); }
        }

        public WhenElement<TEvalContext, TExecContext> When(EvaluateAction<TEvalContext> evaluator)
        {
            var elm = new WhenElement<TEvalContext, TExecContext>(evaluator);
            whenElements.Add(elm);
            return elm;
        }
    }

    /*
     *  Triggers | When | Element | Element | ... 
     * 
     *  State
     *      when_elements := when要素の集合
     *      active_when_elements := 現在のwhen要素の集合
     *      cursor := 現在のマシンの深さ (初期値: 0)
     *      triggers := 反応するべきトリガのセット
     *      
     *      Init()
     *      
     *      
     *      
     *      Input(input = 入力されたイベント)
     *      
     *          if input in triggers:
     *              active_when_elements = filter(when_elements, _.evaluator() )
     *          
     *              if input is FireEvent:
     *                  この深さの
     *                  for elm in active_when_elements[cursor+1].on_fire_elements.trigger == input:
     *                      elm.executor()
     *                  
     *              elif input is PressEvent:
     *                  次の深さのwhen_elementsでOnPressかOnStrokeに対応していれば、
     *                      フィルタリングして次の深さへ
     *
     *      
     * 
     */

    /* WhenElement
     * 
     * .On(FireEvent) -> new SingleThrowElement
     * 
     * .On(PressEvent) -> new DoubleThrowElement
     */
    public class WhenElement<TEvalContext, TExecContext> : Element
        where TEvalContext : EvaluationContext
        where TExecContext : ExecutionContext
    {
        public override bool IsFull
        {
            get => WhenEvaluator != null &&
                SingleThrowElements.Any(e => e.IsFull) ||
                DoubleThrowElements.Any(e => e.IsFull);
        }

        public EvaluateAction<TEvalContext> WhenEvaluator { get; private set; }

        private List<SingleThrowElement<TExecContext>> singleThrowElements = new List<SingleThrowElement<TExecContext>>();
        public IReadOnlyCollection<SingleThrowElement<TExecContext>> SingleThrowElements
        {
            get { return singleThrowElements.ToList(); }
        }

        private List<DoubleThrowElement<TExecContext>> doubleThrowElements = new List<DoubleThrowElement<TExecContext>>();
        public IReadOnlyCollection<DoubleThrowElement<TExecContext>> DoubleThrowElements
        {
            get { return doubleThrowElements.ToList(); }
        }

        public WhenElement(EvaluateAction<TEvalContext> evaluator)
        {
            WhenEvaluator = evaluator;
        }

        public SingleThrowElement<TExecContext> On(IFireEvent triggerEvent)
        {
            var elm = new SingleThrowElement<TExecContext>(triggerEvent);
            singleThrowElements.Add(elm);
            return elm;
        }

        public DoubleThrowElement<TExecContext> On(IPressEvent triggerEvent)
        {
            var elm = new DoubleThrowElement<TExecContext>(triggerEvent);
            doubleThrowElements.Add(elm);
            return elm;
        }
    }

    /* SingleThrowElement
     * 
     * .Do() -> this
     */
    public class SingleThrowElement<T> : Element
        where T : ExecutionContext
    {
        public override bool IsFull
        {
            get => Trigger != null &&
                DoExecutors.Count > 0 && DoExecutors.Any(e => e != null);
        }

        public readonly IFireEvent Trigger;
        
        private List<ExecuteAction<T>> doExecutors = new List<ExecuteAction<T>>();
        public IReadOnlyCollection<ExecuteAction<T>> DoExecutors
        {
            get { return doExecutors.ToList(); }
        }
        
        public SingleThrowElement(IFireEvent triggerEvent)
        {
            Trigger = triggerEvent;
        }

        public SingleThrowElement<T> Do(ExecuteAction<T> executor)
        {
            doExecutors.Add(executor);
            return this;
        }
    }

    /* 
     * .Press() -> this 
     * 
     * .Do() -> this 
     * 
     * .Release() -> this 
     * 
     * .On(FireEvent) -> new SingleThrowElement
     * 
     * .On(PressEvent) -> new DoubleThrowElement
     * 
     * .On(StrokeEvent) -> new StrokeEelement
     */
    public class DoubleThrowElement<T> : Element
        where T : ExecutionContext
    {
        public override bool IsFull
        {
            get => Trigger != null &&
                PressExecutors.Count > 0 && PressExecutors.Any(e => e != null) ||
                DoExecutors.Count > 0 && DoExecutors.Any(e => e != null) ||
                ReleaseExecutors.Count > 0 && ReleaseExecutors.Any(e => e != null) ||
                SingleThrowElements.Any(e => e.IsFull) ||
                DoubleThrowElements.Any(e => e.IsFull) ||
                StrokeElements.Any(e => e.IsFull);
        }

        public readonly IPressEvent Trigger;

        private List<SingleThrowElement<T>> singleThrowElements = new List<SingleThrowElement<T>>();
        public IReadOnlyCollection<SingleThrowElement<T>> SingleThrowElements
        {
            get { return singleThrowElements.ToList(); }
        }

        private List<DoubleThrowElement<T>> doubleThrowElements = new List<DoubleThrowElement<T>>();
        public IReadOnlyCollection<DoubleThrowElement<T>> DoubleThrowElements
        {
            get { return doubleThrowElements.ToList(); }
        }

        private List<StrokeElement<T>> strokeElements = new List<StrokeElement<T>>();
        public IReadOnlyCollection<StrokeElement<T>> StrokeElements
        {
            get { return strokeElements.ToList(); }
        }

        private List<ExecuteAction<T>> pressExecutors = new List<ExecuteAction<T>>();
        public IReadOnlyCollection<ExecuteAction<T>> PressExecutors
        {
            get { return pressExecutors.ToList(); }
        }

        private List<ExecuteAction<T>> doExecutors = new List<ExecuteAction<T>>();
        public IReadOnlyCollection<ExecuteAction<T>> DoExecutors
        {
            get { return doExecutors.ToList(); }
        }

        private List<ExecuteAction<T>> releaseExecutors = new List<ExecuteAction<T>>();
        public IReadOnlyCollection<ExecuteAction<T>> ReleaseExecutors
        {
            get { return releaseExecutors.ToList(); }
        }

        public DoubleThrowElement(IPressEvent triggerEvent)
        {
            Trigger = triggerEvent;
        }

        public SingleThrowElement<T> On(IFireEvent triggerEvent)
        {
            var elm = new SingleThrowElement<T>(triggerEvent);
            singleThrowElements.Add(elm);
            return elm;
        }

        public DoubleThrowElement<T> On(IPressEvent triggerEvent)
        {
            var elm = new DoubleThrowElement<T>(triggerEvent);
            doubleThrowElements.Add(elm);
            return elm;
        }

        public StrokeElement<T> On(params StrokeEvent.Direction[] strokeDirections)
        {
            var elm = new StrokeElement<T>(strokeDirections);
            strokeElements.Add(elm);
            return elm;
        }

        public DoubleThrowElement<T> Press(ExecuteAction<T> executor)
        {
            pressExecutors.Add(executor);
            return this;
        }

        public DoubleThrowElement<T> Do(ExecuteAction<T> executor)
        {
            doExecutors.Add(executor);
            return this;
        }

        public DoubleThrowElement<T> Release(ExecuteAction<T> executor)
        {
            releaseExecutors.Add(executor);
            return this;
        }
    }

    /* 
     * .Do() -> this 
     */
    public class StrokeElement<T> : Element
        where T : ExecutionContext
    {
        public override bool IsFull
        {
            get => Strokes != null && Strokes.Count > 0 &&
                DoExecutors.Count > 0 && DoExecutors.Any(e => e != null); 
        }

        public IReadOnlyCollection<StrokeEvent.Direction> Strokes { get; private set; }

        private List<ExecuteAction<T>> doExecutors = new List<ExecuteAction<T>>();
        public IReadOnlyCollection<ExecuteAction<T>> DoExecutors
        {
            get { return doExecutors.ToList(); }
        }

        public StrokeElement(params StrokeEvent.Direction[] strokes)
        {
            Strokes = strokes;
        }

        public StrokeElement<T> Do(ExecuteAction<T> executor)
        {
            doExecutors.Add(executor);
            return this;
        }
    }


}
