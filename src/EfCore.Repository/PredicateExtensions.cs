using System.Linq.Expressions;

namespace EfCore.Repository.Expressions
{
    /// <summary>
    /// Combines conditions of the form <c>Expression&lt;Func&lt;TEntity, bool&gt;&gt;</c> into one condition that the
    /// database provider can translate, for example to pass an OR of two conditions to
    /// <see cref="SpecificationBuilderExtensions.Where{TEntity}(Specification{TEntity}, Expression{Func{TEntity, bool}})"/>
    /// or to a repository filter. The parameter of the right-hand condition is rebound to the parameter of the left-hand
    /// one, so the result is a single lambda without an invocation of another lambda.
    /// </summary>
    public static class PredicateExtensions
    {
        /// <summary>
        /// Returns a condition that is <see langword="true"/> when both <paramref name="left"/> and
        /// <paramref name="right"/> are <see langword="true"/> (a short-circuit AND, translated to SQL <c>AND</c>).
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="left">The first condition.</param>
        /// <param name="right">The second condition.</param>
        /// <returns>A new condition; the arguments are not changed.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="left"/> or <paramref name="right"/> is <see langword="null"/>.</exception>
        public static Expression<Func<TEntity, bool>> And<TEntity>(this Expression<Func<TEntity, bool>> left, Expression<Func<TEntity, bool>> right)
            => Combine(left, right, Expression.AndAlso);

        /// <summary>
        /// Returns a condition that is <see langword="true"/> when <paramref name="left"/> or <paramref name="right"/>
        /// (or both) is <see langword="true"/> (a short-circuit OR, translated to SQL <c>OR</c>).
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="left">The first condition.</param>
        /// <param name="right">The second condition.</param>
        /// <returns>A new condition; the arguments are not changed.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="left"/> or <paramref name="right"/> is <see langword="null"/>.</exception>
        public static Expression<Func<TEntity, bool>> Or<TEntity>(this Expression<Func<TEntity, bool>> left, Expression<Func<TEntity, bool>> right)
            => Combine(left, right, Expression.OrElse);

        /// <summary>
        /// Returns a condition that is <see langword="true"/> when <paramref name="condition"/> is
        /// <see langword="false"/> (translated to SQL <c>NOT</c>).
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="condition">The condition to negate.</param>
        /// <returns>A new condition; the argument is not changed.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="condition"/> is <see langword="null"/>.</exception>
        public static Expression<Func<TEntity, bool>> Not<TEntity>(this Expression<Func<TEntity, bool>> condition)
        {
            if (condition == null)
            {
                throw new ArgumentNullException(nameof(condition));
            }

            return Expression.Lambda<Func<TEntity, bool>>(Expression.Not(condition.Body), condition.Parameters);
        }

        private static Expression<Func<TEntity, bool>> Combine<TEntity>(
            Expression<Func<TEntity, bool>> left,
            Expression<Func<TEntity, bool>> right,
            Func<Expression, Expression, BinaryExpression> combine)
        {
            if (left == null)
            {
                throw new ArgumentNullException(nameof(left));
            }

            if (right == null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            ParameterExpression parameter = left.Parameters[0];
            Expression rightBody = new ParameterRebinder(right.Parameters[0], parameter).Visit(right.Body);

            return Expression.Lambda<Func<TEntity, bool>>(combine(left.Body, rightBody), parameter);
        }

        private sealed class ParameterRebinder : ExpressionVisitor
        {
            private readonly ParameterExpression _from;
            private readonly ParameterExpression _to;

            public ParameterRebinder(ParameterExpression from, ParameterExpression to)
            {
                _from = from;
                _to = to;
            }

            protected override Expression VisitParameter(ParameterExpression node)
                => node == _from ? _to : base.VisitParameter(node);
        }
    }
}
