﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using CreviceApp.User;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreviceApp.DSL.Tests
{
    [TestClass()]
    public class DoElementTests
    {
        [TestMethod()]
        public void funcTest()
        {
            var ctx = new Core.UserActionExecutionContext(new Point());
            var root = new Root();
            var appElement = root.@when(_ => true);
            var onElement = appElement.@on(new Def.RightButton());
            var ifElement = onElement.@if(new Def.MoveDown(), new Def.MoveRight());
            var called = false;
            var doEmenent = ifElement.@do(_ => { called = true; });
            Assert.IsFalse(called);
            root.whenElements[0].onElements[0].ifStrokeElements[0].doElements[0].func(ctx);
            Assert.IsTrue(called);
        }
    }
}