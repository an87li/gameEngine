/*********************************************************************************

Copyright (c) 2014 Bionic Dog Studios LLC

*********************************************************************************/

/*!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
!!!!!!!!!This is an auto-generated file.  Any changes will be destroyed!!!!!!!!!!!
!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!*/


using System;
using System.IO;

using OpenTK.Input;


using Util;
using Events;


namespace Events
{
	public class MouseButtonUpEvent : Event
	{
		static EventName theName;

		MouseButton myButton;
		Int32 myX;
		Int32 myY;
		Util.KeyModifiers myModifiers;

		public MouseButtonUpEvent(): base() { myName=theName; }
		public MouseButtonUpEvent(MouseButton button, Int32 x, Int32 y, Util.KeyModifiers modifiers) : this(button, x, y, modifiers, TimeSource.defaultClock.currentTime(), 0.0) { }
		public MouseButtonUpEvent(MouseButton button, Int32 x, Int32 y, Util.KeyModifiers modifiers, double timeStamp) : this(button, x, y, modifiers, timeStamp, 0.0) { }
		public MouseButtonUpEvent(MouseButton button, Int32 x, Int32 y, Util.KeyModifiers modifiers, double timeStamp, double delay)
		: base(timeStamp, delay)
		{
			myName = theName;
			myButton=button;
			myX=x;
			myY=y;
			myModifiers=modifiers;
		}

		static MouseButtonUpEvent()
		{
			theName = new EventName("input.mouse.button.up");
			
		}


		public MouseButton button
		{
			get { return myButton;}
		}
	
		public Int32 x
		{
			get { return myX;}
		}
	
		public Int32 y
		{
			get { return myY;}
		}
	
		public Util.KeyModifiers modifiers
		{
			get { return myModifiers;}
		}
	







	}
	
}

	