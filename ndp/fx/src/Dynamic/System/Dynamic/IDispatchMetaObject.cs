﻿/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Microsoft Public License. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the  Microsoft Public License, please send an email to 
 * dlr@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Microsoft Public License.
 *
 * You must not remove this notice, or any other, from this software.
 *
 *
 * ***************************************************************************/

#if !SILVERLIGHT // ComObject

using System.Collections.Generic;
using System.Dynamic;
using System.Dynamic.Utils;
using System.Linq.Expressions;

namespace System.Dynamic {

    internal sealed class IDispatchMetaObject : ComFallbackMetaObject {
        private readonly IDispatchComObject _self;

        internal IDispatchMetaObject(Expression expression, IDispatchComObject self)
            : base(expression, BindingRestrictions.Empty, self) {
            _self = self;
        }

        public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args) {
            ContractUtils.RequiresNotNull(binder, "binder");

            ComMethodDesc method;
            if (_self.TryGetMemberMethod(binder.Name, out method) ||
                _self.TryGetMemberMethodExplicit(binder.Name, out method)) {

                IList<ArgumentInfo> argInfos = binder.Arguments;
                ComBinderHelpers.ProcessArgumentsForCom(ref args, ref argInfos);

                return BindComInvoke(args, method, argInfos);
            }

            return base.BindInvokeMember(binder, args);
        }

        public override DynamicMetaObject BindInvoke(InvokeBinder binder, DynamicMetaObject[] args) {
            ContractUtils.RequiresNotNull(binder, "binder");

            ComMethodDesc method;
            if (_self.TryGetGetItem(out method)) {
                IList<ArgumentInfo> argInfos = binder.Arguments;
                ComBinderHelpers.ProcessArgumentsForCom(ref args, ref argInfos);

                return BindComInvoke(args, method, argInfos);
            }

            return base.BindInvoke(binder, args);
        }

        private DynamicMetaObject BindComInvoke(DynamicMetaObject[] args, ComMethodDesc method, IList<ArgumentInfo> arguments) {
            return new ComInvokeBinder(
                arguments,
                args,
                IDispatchRestriction(),
                Expression.Constant(method),
                Expression.Property(
                    Helpers.Convert(Expression, typeof(IDispatchComObject)),
                    typeof(IDispatchComObject).GetProperty("DispatchObject")
                ),
                method
            ).Invoke();
        }

        public override DynamicMetaObject BindGetMember(GetMemberBinder binder) {
            ContractUtils.RequiresNotNull(binder, "binder");

            ComMethodDesc method;
            ComEventDesc @event;

            // 1. Try methods
            if (_self.TryGetMemberMethod(binder.Name, out method)) {
                return BindGetMember(method);
            }

            // 2. Try events
            if (_self.TryGetMemberEvent(binder.Name, out @event)) {
                return BindEvent(@event);
            }

            // 3. Try methods explicitly by name
            if (_self.TryGetMemberMethodExplicit(binder.Name, out method)) {
                return BindGetMember(method);
            }

            // 4. Fallback
            return base.BindGetMember(binder);
        }

        private DynamicMetaObject BindGetMember(ComMethodDesc method) {
            if (method.IsDataMember) {
                if (method.ParamCount == 0) {
                    return BindComInvoke(DynamicMetaObject.EmptyMetaObjects, method, new ArgumentInfo[0]);
                }
            }

            return new DynamicMetaObject(
                Expression.Call(
                    typeof(ComRuntimeHelpers).GetMethod("CreateDispCallable"),
                    Helpers.Convert(Expression, typeof(IDispatchComObject)),
                    Expression.Constant(method)
                ),
                IDispatchRestriction()
            );
        }

        private DynamicMetaObject BindEvent(ComEventDesc @event) {
            // BoundDispEvent CreateComEvent(object rcw, Guid sourceIid, int dispid)
            Expression result =
                Expression.Call(
                    typeof(ComRuntimeHelpers).GetMethod("CreateComEvent"),
                    ComObject.RcwFromComObject(Expression),
                    Expression.Constant(@event.sourceIID),
                    Expression.Constant(@event.dispid)
                );

            return new DynamicMetaObject(
                result,
                IDispatchRestriction()
            );
        }

        public override DynamicMetaObject BindGetIndex(GetIndexBinder binder, DynamicMetaObject[] indexes) {
            ContractUtils.RequiresNotNull(binder, "binder");


            ComMethodDesc getItem;
            if (_self.TryGetGetItem(out getItem)) {
                IList<ArgumentInfo> argInfos = binder.Arguments;
                ComBinderHelpers.ProcessArgumentsForCom(ref indexes, ref argInfos);

                return BindComInvoke(indexes, getItem, argInfos);
            }

            return base.BindGetIndex(binder, indexes);
        }

        public override DynamicMetaObject BindSetIndex(SetIndexBinder binder, DynamicMetaObject[] indexes, DynamicMetaObject value) {
            ContractUtils.RequiresNotNull(binder, "binder");

            ComMethodDesc setItem;
            if (_self.TryGetSetItem(out setItem)) {
                IList<ArgumentInfo> argInfos = binder.Arguments;
                ComBinderHelpers.ProcessArgumentsForCom(ref indexes, ref argInfos);

                // add an arginfo for the value
                argInfos = argInfos.AddLast(Expression.PositionalArg(argInfos.Count));

                return BindComInvoke(indexes.AddLast(value), setItem, argInfos);
            }

            return base.BindSetIndex(binder, indexes, value);
        }

        public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value) {
            ContractUtils.RequiresNotNull(binder, "binder");

            return
                // 1. Check for simple property put
                TryPropertyPut(binder, value) ??

                // 2. Check for event handler hookup where the put is dropped
                TryEventHandlerNoop(binder, value) ??

                // 3. Fallback
                base.BindSetMember(binder, value);
        }

        private DynamicMetaObject TryPropertyPut(SetMemberBinder binder, DynamicMetaObject value) {
            ComMethodDesc method;
            bool holdsNull = value.Value == null && value.HasValue;
            if (_self.TryGetPropertySetter(binder.Name, out method, value.LimitType, holdsNull) ||
                _self.TryGetPropertySetterExplicit(binder.Name, out method, value.LimitType, holdsNull)) {
                BindingRestrictions restrictions = IDispatchRestriction();
                Expression dispatch =
                    Expression.Property(
                        Helpers.Convert(Expression, typeof(IDispatchComObject)),
                        typeof(IDispatchComObject).GetProperty("DispatchObject")
                    );

                return new ComInvokeBinder(
                    new ArgumentInfo[] { Expression.PositionalArg(0) },
                    new[] { value },
                    restrictions,
                    Expression.Constant(method),
                    dispatch,
                    method
                ).Invoke();
            }

            return null;
        }

        private DynamicMetaObject TryEventHandlerNoop(SetMemberBinder binder, DynamicMetaObject value) {
            ComEventDesc @event;
            if (_self.TryGetMemberEvent(binder.Name, out @event) && value.LimitType == typeof(BoundDispEvent)) {
                // Drop the event property set.
                return new DynamicMetaObject(
                    Expression.Constant(null),
                    value.Restrictions.Merge(IDispatchRestriction()).Merge(BindingRestrictions.GetTypeRestriction(value.Expression, typeof(BoundDispEvent)))
                );
            }

            return null;
        }

        private BindingRestrictions IDispatchRestriction() {
            return IDispatchRestriction(Expression, _self.ComTypeDesc);
        }

        internal static BindingRestrictions IDispatchRestriction(Expression expr, ComTypeDesc typeDesc) {
            return BindingRestrictions.GetTypeRestriction(
                expr, typeof(IDispatchComObject)
            ).Merge(
                BindingRestrictions.GetExpressionRestriction(
                    Expression.Equal(
                        Expression.Property(
                            Helpers.Convert(expr, typeof(IDispatchComObject)),
                            typeof(IDispatchComObject).GetProperty("ComTypeDesc")
                        ),
                        Expression.Constant(typeDesc)
                    )
                )
            );
        }

        protected override ComUnwrappedMetaObject UnwrapSelf() {
            return new ComUnwrappedMetaObject(
                ComObject.RcwFromComObject(Expression),
                IDispatchRestriction(),
                _self.RuntimeCallableWrapper
            );
        }
    }
}

#endif
