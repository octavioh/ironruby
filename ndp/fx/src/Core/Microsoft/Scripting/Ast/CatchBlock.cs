/* ****************************************************************************
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

using System.Dynamic.Utils;

namespace System.Linq.Expressions {

    /// <summary>
    /// Represents a catch statement in a try block. 
    /// This must have the same return type (i.e., the type of <see cref="P:CatchBlock.Body"/>) as the try block it is associated with.
    /// </summary>
    public sealed class CatchBlock {
        private readonly Type _test;
        private readonly ParameterExpression _var;
        private readonly Expression _body;
        private readonly Expression _filter;

        internal CatchBlock(Type test, ParameterExpression variable, Expression body, Expression filter) {
            _test = test;
            _var = variable;
            _body = body;
            _filter = filter;
        }

        /// <summary>
        /// Gets a reference to the <see cref="Exception"/> object caught by this handler.
        /// </summary>
        public ParameterExpression Variable {
            get { return _var; }
        }

        /// <summary>
        /// Gets the type of <see cref="Exception"/> this handler catches.
        /// </summary>
        public Type Test {
            get { return _test; }
        }

        /// <summary>
        /// Gets the body of the catch block.
        /// </summary>
        public Expression Body {
            get { return _body; }
        }

        /// <summary>
        /// Gets the body of the <see cref="CatchBlock"/>'s filter.
        /// </summary>
        public Expression Filter {
            get {
                return _filter;
            }
        }
    }

    public partial class Expression {
        /// <summary>
        /// Creates a <see cref="CatchBlock"/> representing a catch statement. 
        /// The <see cref="Type"/> of <see cref="Exception"/> to be caught can be specified but no reference to the <see cref="Exception"/> object 
        /// will be available for use in the <see cref="CatchBlock"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> of <see cref="Exception"/> this <see cref="CatchBlock"/> will handle.</param>
        /// <param name="body">The body of the catch statement.</param>
        /// <returns>The created <see cref="CatchBlock"/>.</returns>
        public static CatchBlock Catch(Type type, Expression body) {
            return MakeCatchBlock(type, null, body, null);
        }

        /// <summary>
        /// Creates a <see cref="CatchBlock"/> representing a catch statement with a reference to the caught <see cref="Exception"/> object for use in the handler body.
        /// </summary>
        /// <param name="variable">A <see cref="ParameterExpression"/> representing a reference to the <see cref="Exception"/> object caught by this handler.</param>
        /// <param name="body">The body of the catch statement.</param>
        /// <returns>The created <see cref="CatchBlock"/>.</returns>
        public static CatchBlock Catch(ParameterExpression variable, Expression body) {
            ContractUtils.RequiresNotNull(variable, "variable");
            return MakeCatchBlock(variable.Type, variable, body, null);
        }

        /// <summary>
        /// Creates a <see cref="CatchBlock"/> representing a catch statement with 
        /// an <see cref="Exception"/> filter but no reference to the caught <see cref="Exception"/> object.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> of <see cref="Exception"/> this <see cref="CatchBlock"/> will handle.</param>
        /// <param name="body">The body of the catch statement.</param>
        /// <param name="filter">The body of the <see cref="Exception"/> filter.</param>
        /// <returns>The created <see cref="CatchBlock"/>.</returns>
        public static CatchBlock Catch(Type type, Expression body, Expression filter) {
            return MakeCatchBlock(type, null, body, filter);
        }

        /// <summary>
        /// Creates a <see cref="CatchBlock"/> representing a catch statement with 
        /// an <see cref="Exception"/> filter and a reference to the caught <see cref="Exception"/> object.
        /// </summary>
        /// <param name="variable">A <see cref="ParameterExpression"/> representing a reference to the <see cref="Exception"/> object caught by this handler.</param>
        /// <param name="body">The body of the catch statement.</param>
        /// <param name="filter">The body of the <see cref="Exception"/> filter.</param>
        /// <returns>The created <see cref="CatchBlock"/>.</returns>
        public static CatchBlock Catch(ParameterExpression variable, Expression body, Expression filter) {
            ContractUtils.RequiresNotNull(variable, "variable");
            return MakeCatchBlock(variable.Type, variable, body, filter);
        }

        /// <summary>
        /// Creates a <see cref="CatchBlock"/> representing a catch statement with the specified elements.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> of <see cref="Exception"/> this <see cref="CatchBlock"/> will handle.</param>
        /// <param name="variable">A <see cref="ParameterExpression"/> representing a reference to the <see cref="Exception"/> object caught by this handler.</param>
        /// <param name="body">The body of the catch statement.</param>
        /// <param name="filter">The body of the <see cref="Exception"/> filter.</param>
        /// <returns>The created <see cref="CatchBlock"/>.</returns>
        /// <remarks><paramref name="type"/> must be non-null and match the type of <paramref name="variable"/> (if it is supplied).</remarks>
        public static CatchBlock MakeCatchBlock(Type type, ParameterExpression variable, Expression body, Expression filter) {
            ContractUtils.RequiresNotNull(type, "type");
            ContractUtils.Requires(variable == null || variable.Type == type, "variable");
            if (variable != null && variable.IsByRef) {
                throw Error.VariableMustNotBeByRef(variable, variable.Type);
            }
            RequiresCanRead(body, "body");
            if (filter != null) {
                RequiresCanRead(filter, "filter");
                ContractUtils.Requires(filter.Type == typeof(bool), Strings.ArgumentMustBeBoolean);
            }

            return new CatchBlock(type, variable, body, filter);
        }
    }
}
