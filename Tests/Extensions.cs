using Moq.Language;
using Moq.Language.Flow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tests.Util;

namespace Tests
{
    static class Extensions
    {
        public delegate void OutAction<TOut>(out TOut outVal);
        public delegate void OutAction<in T1, TOut>(T1 arg1, out TOut outVal);

        public static IReturnsThrows<TMock, TReturn> OutCallback<TMock, TReturn, TOut>(this ICallback<TMock, TReturn> mock, OutAction<TOut> action)
            where TMock : class
        {
            return OutCallbackInternal(mock, action);
        }

        public static IReturnsThrows<TMock, TReturn> OutCallback<TMock, TReturn, T1, TOut>(this ICallback<TMock, TReturn> mock, OutAction<T1, TOut> action)
            where TMock : class
        {
            return OutCallbackInternal(mock, action);
        }

        private static IReturnsThrows<TMock, TReturn> OutCallbackInternal<TMock, TReturn>(ICallback<TMock, TReturn> mock, object action)
            where TMock : class
        {
            mock.GetType()
                .Assembly.GetType("Moq.MethodCall")
                .InvokeMember("SetCallbackWithArguments", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance, null, mock,
                    new[] { action });
            return mock as IReturnsThrows<TMock, TReturn>;
        }

        //public static EventWatcher CaptureEvent<T>(this T o, Func<T, string> act)
        //{
        //    var ev = typeof(T).GetEvent(act(o));

        //    //var delegateSignature = typeof(D).GetMethod("Invoke");
        //    //var eventSignature = ev.EventHandlerType.GetMethod("Invoke");

        //    //var delegateParams = delegateSignature.GetParameters();
        //    //var eventParams = eventSignature.GetParameters();

        //    //if (!(delegateParams.Length == eventParams.Length && delegateParams.Zip(eventParams, (a, b) => a.ParameterType == b.ParameterType).All(x => x)))
        //    //    throw new ArgumentException();

        //    return new EventWatcher(o, ev);
        //}
    }


    //class EventWatcher
    //{
    //    protected readonly object _obj;
    //    protected readonly EventInfo _eventInfo;

    //    public EventWatcher(object obj, EventInfo eventInfo)
    //    {
    //        _obj = obj;
    //        _eventInfo = eventInfo;
    //    }
    //}

    //class EventWatcher<T> : EventWatcher
    //{
    //    public EventWatcher(object obj, EventInfo eventInfo)
    //        : base(obj, eventInfo)
    //    {
    //    }

    //    public T WaitForEvent()
    //    {
    //    }
    //}

    class EventWatcher
    {
        //public EventWatcher(object obj, EventInfo eventInfo)
        //{
        //    _obj = obj;
        //    _eventInfo = eventInfo;
        //}

        public static EventWatcher<T> For<T>(T obj)
        {
            return new EventWatcher<T>(obj);
        }
    }

    class EventWatcher<T> : IDisposable
    {
        class WatchingInfo
        {
            public EventInfo Event { get; }
            public MethodInfo Signature { get; }
            public bool ShouldFire { get; set; }
            public int FiredCount { get; set; }

            public WatchingInfo(EventInfo ev)
            {
                this.Event = ev;
                this.Signature = ev.EventHandlerType.GetMethod("Invoke");
            }

            public bool Verify()
            {
                return (this.ShouldFire && this.FiredCount > 0) || (!this.ShouldFire && this.FiredCount == 0);
            }

            public void Expecting(bool shouldFire)
            {
                this.ShouldFire = shouldFire;
            }
        }

        readonly ManualResetEvent _allRequiredEventsFiredEv = new ManualResetEvent(false);
        readonly Dictionary<EventInfo, WatchingInfo> _events;
        readonly Dictionary<MethodBase, WatchingInfo> _eventsByAccessors;
        readonly object _obj;

        public EventWatcher(object obj)
        {
            _events = obj.GetType().GetEvents(BindingFlags.Public|BindingFlags.Instance).ToDictionary(e => e, e => new WatchingInfo(e));
            _eventsByAccessors = _events.Values.SelectMany(e => new (WatchingInfo e, MethodBase m)[] { (e, e.Event.AddMethod), (e, e.Event.RemoveMethod) })
                                        .ToDictionary(e => e.m, e => e.e);
            _obj = obj;
            this.SetupHandlers();
        }

        public EventWatcher<T> Expecting(Action<T> act)
        {
            var il = act.Method.ReadIlCode();
            var ev = il.OfType<IlOp<MethodBase>>().Select(op => _eventsByAccessors[op.Operand]).First();
            ev.Expecting(true);

            return this;
        }

        private void SetupHandlers()
        {
            Func<WatchingInfo, object[], object> realHandler = this.OnEvent;

            foreach (var info in _events.Values)
            {
                var handlerArgs = info.Signature.GetParameters().Select(p => new { p, e = Expression.Parameter(p.ParameterType, p.Name) }).ToArray();
                var resultVar = Expression.Variable(typeof(object), "result");
                var handlerExpr = Expression.Lambda(
                    info.Event.EventHandlerType,
                    Expression.Block(
                        new[] { resultVar },


                        Expression.Assign(resultVar, Expression.Invoke(
                            Expression.Constant(realHandler),
                            Expression.Constant(info),
                            Expression.NewArrayInit(typeof(object), handlerArgs.Select(a => Expression.Convert(a.e, typeof(object))).ToArray())
                        )),
                        info.Signature.ReturnType == typeof(void) ? (Expression)Expression.Empty() : (
                            Expression.IfThenElse(
                                Expression.Equal(resultVar, Expression.Constant(null)),
                                Expression.Default(info.Signature.ReturnType),
                                Expression.Convert(resultVar, info.Signature.ReturnType)
                            )
                        )
                    ),
                    handlerArgs.Select(a => a.e)
                );

                var handler = handlerExpr.Compile();
                info.Event.AddEventHandler(_obj, handler);
            }
        }

        private object OnEvent(WatchingInfo info, object[] args)
        {
            info.FiredCount++;

            if (_events.Values.Where(e => e.ShouldFire).All(e => e.FiredCount > 0))
                _allRequiredEventsFiredEv.Set();

            return null;
        }

        public bool WaitForAllRequiredEvents(TimeSpan timeout)
        {
            return _allRequiredEventsFiredEv.WaitOne(timeout);
        }

        public void WaitForAllRequiredEventsOrThrow(TimeSpan? timeout = null)
        {
            if (!this.WaitForAllRequiredEvents(timeout ?? TimeSpan.FromSeconds(15)))
            {
                var missingEvents = _events.Values.Where(e => e.ShouldFire && e.FiredCount == 0).Select(e => e.Event.Name).ToArray();
                throw new Exception($"Failed to wait until all required events will be raized. ({string.Join(", ", missingEvents)})");
            }
        }

        public void Dispose()
        {
            var failedEvents = _events.Values.Where(e => !e.Verify()).ToArray();
            if (failedEvents.Any())
                throw new Exception($"A number of expectations failed for some events of [{_obj}]: " + string.Join(", ", failedEvents.Select(e => e.Event.Name)));
        }
    }
}
