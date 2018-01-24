﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crevice.Future
{
    public class Events
    {
        public readonly MoveEventEvent Move;
        public readonly LeftButtonDownEvent LeftButtonDownEvent;
        public readonly LeftButtonUpEvent LeftButtonUpEvent;
        public readonly MiddleButtonDownEvent MiddleButtonDownEvent;
        public readonly MiddleButtonUpEvent MiddleButtonUpEvent;
        public readonly RightButtonDownEvent RightButtonDownEvent;
        public readonly RightButtonUpEvent RightButtonUpEvent;
        public readonly WheelDownEvent WheelDownEvent;
        public readonly WheelUpEvent WheelUpEvent;
        public readonly WheelLeftEvent WheelLeftEvent;
        public readonly WheelRightEvent WheelRightEvent;
        public readonly X1ButtonDownEvent X1ButtonDownEvent;
        public readonly X1ButtonUpEvent X1ButtonUpEvent;
        public readonly X2ButtonDownEvent X2ButtonDownEvent;
        public readonly X2ButtonUpEvent X2ButtonUpEvent;

        public readonly PhysicalLeftButtonDownEvent LeftButtonDownP0Event;
        public readonly PhysicalLeftButtonUpEvent LeftButtonUpP0Event;
        public Events()
        {
            var id = 0;
            // 0 is reserved for StrokeEvent.
            Move = new MoveEventEvent(++id);
            LeftButtonDownEvent = new LeftButtonDownEvent(++id);
            LeftButtonUpEvent = new LeftButtonUpEvent(++id);
            MiddleButtonDownEvent = new MiddleButtonDownEvent(++id);
            MiddleButtonUpEvent = new MiddleButtonUpEvent(++id);
            RightButtonDownEvent = new RightButtonDownEvent(++id);
            RightButtonUpEvent = new RightButtonUpEvent(++id);
            WheelDownEvent = new WheelDownEvent(++id);
            WheelUpEvent = new WheelUpEvent(++id);
            WheelLeftEvent = new WheelLeftEvent(++id);
            WheelRightEvent = new WheelRightEvent(++id);
            X1ButtonDownEvent = new X1ButtonDownEvent(++id);
            X1ButtonUpEvent = new X1ButtonUpEvent(++id);
            X2ButtonDownEvent = new X2ButtonDownEvent(++id);
            X2ButtonUpEvent = new X2ButtonUpEvent(++id);

            id = 99;
            LeftButtonDownP0Event = new PhysicalLeftButtonDownEvent(++id);
            LeftButtonUpP0Event = new PhysicalLeftButtonUpEvent(++id);
        }

        private static Events singleton = new Events();
        public static Events Constants
        {
            get { return singleton; }
        }
    }

    public class MoveEventEvent : FireEvent<MoveSwich>
    {
        public MoveEventEvent(int eventId) : base(eventId) { }
    }

    public class LeftButtonDownEvent : PressEvent<LeftButtonSwitch>
    {
        public LeftButtonDownEvent(int eventId) : base(eventId) { }

        public override ReleaseEvent<LeftButtonSwitch> OppositePressEvent
        { get { return Events.Constants.LeftButtonUpEvent; } }
    }

    public class LeftButtonUpEvent : ReleaseEvent<LeftButtonSwitch>
    {
        public LeftButtonUpEvent(int eventId) : base(eventId) { }

        public override PressEvent<LeftButtonSwitch> OppositeReleaseEvent
        { get { return Events.Constants.LeftButtonDownEvent; } }
    }
    
    public class MiddleButtonDownEvent : PressEvent<MiddleButtonSwitch>
    {
        public MiddleButtonDownEvent(int eventId) : base(eventId) { }

        public override ReleaseEvent<MiddleButtonSwitch> OppositePressEvent
        { get { return Events.Constants.MiddleButtonUpEvent; } }
    }

    public class MiddleButtonUpEvent : ReleaseEvent<MiddleButtonSwitch>
    {
        public MiddleButtonUpEvent(int eventId) : base(eventId) { }

        public override PressEvent<MiddleButtonSwitch> OppositeReleaseEvent
        { get { return Events.Constants.MiddleButtonDownEvent; } }
    }

    public class RightButtonDownEvent : PressEvent<RightButtonSwitch>
    {
        public RightButtonDownEvent(int eventId) : base(eventId) { }

        public override ReleaseEvent<RightButtonSwitch> OppositePressEvent
        { get { return Events.Constants.RightButtonUpEvent; } }
    }

    public class RightButtonUpEvent : ReleaseEvent<RightButtonSwitch>
    {
        public RightButtonUpEvent(int eventId) : base(eventId) { }

        public override PressEvent<RightButtonSwitch> OppositeReleaseEvent
        { get { return Events.Constants.RightButtonDownEvent; } }
    }

    public class WheelDownEvent : FireEvent<MoveSwich>
    {
        public WheelDownEvent(int eventId) : base(eventId) { }
    }

    public class WheelUpEvent : FireEvent<MoveSwich>
    {
        public WheelUpEvent(int eventId) : base(eventId) { }
    }

    public class WheelLeftEvent : FireEvent<MoveSwich>
    {
        public WheelLeftEvent(int eventId) : base(eventId) { }
    }

    public class WheelRightEvent : FireEvent<MoveSwich>
    {
        public WheelRightEvent(int eventId) : base(eventId) { }
    }
    public class X1ButtonDownEvent : PressEvent<X1ButtonSwitch>
    {
        public X1ButtonDownEvent(int eventId) : base(eventId) { }

        public override ReleaseEvent<X1ButtonSwitch> OppositePressEvent
        { get { return Events.Constants.X1ButtonUpEvent; } }
    }

    public class X1ButtonUpEvent : ReleaseEvent<X1ButtonSwitch>
    {
        public X1ButtonUpEvent(int eventId) : base(eventId) { }

        public override PressEvent<X1ButtonSwitch> OppositeReleaseEvent
        { get { return Events.Constants.X1ButtonDownEvent; } }
    }

    public class X2ButtonDownEvent : PressEvent<X2ButtonSwitch>
    {
        public X2ButtonDownEvent(int eventId) : base(eventId) { }

        public override ReleaseEvent<X2ButtonSwitch> OppositePressEvent
        { get { return Events.Constants.X2ButtonUpEvent; } }
    }

    public class X2ButtonUpEvent : ReleaseEvent<X2ButtonSwitch>
    {
        public X2ButtonUpEvent(int eventId) : base(eventId) { }

        public override PressEvent<X2ButtonSwitch> OppositeReleaseEvent
        { get { return Events.Constants.X2ButtonDownEvent; } }
    }

    public class PhysicalLeftButtonDownEvent : PhysicalPressEvent<LeftButtonSwitch>
    {
        public PhysicalLeftButtonDownEvent(int eventId) : base(eventId) { }
        public override PressEvent<LeftButtonSwitch> LogicalEquivalentPressEvent { get { return Events.Constants.LeftButtonDownEvent; } }
        public override PhysicalReleaseEvent<LeftButtonSwitch> OppositePhysicalReleaseEvent { get { return Events.Constants.LeftButtonUpP0Event; } }
    }

    public class PhysicalLeftButtonUpEvent : PhysicalReleaseEvent<LeftButtonSwitch>
    {
        public PhysicalLeftButtonUpEvent(int eventId) : base(eventId) { }
        public override ReleaseEvent<LeftButtonSwitch> LogicalEquivalentReleaseEvent { get { return Events.Constants.LeftButtonUpEvent; } }
        public override PhysicalPressEvent<LeftButtonSwitch> OppositePhysicalPressEvent { get { return Events.Constants.LeftButtonDownP0Event; } }
    }
}
