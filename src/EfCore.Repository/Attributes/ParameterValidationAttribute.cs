using Base.Repository.Options;
using System.Reflection;

namespace EfCore.Repository.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class ParameterValidationAttribute : Attribute
    {
        public bool? IsServiceProvider { get; set; }
        public bool? IsDatabaseOptions { get; set; }

        public ParameterValidationAttribute(bool isServiceProvider)
        {
            IsServiceProvider = isServiceProvider;
        }

        public ParameterValidationAttribute(bool isServiceProvider, bool isDatabaseOptions)
        {
            IsServiceProvider = isServiceProvider;
            IsDatabaseOptions = isDatabaseOptions;
        }

        public void Validate(MethodInfo method, params object[] parameters)
        {
            var parameterInfos = method.GetParameters();

            if (parameters.Length != parameterInfos.Length)
            {
                throw new ArgumentException("Parameter count mismatch.");
            }

            for (int i = 0; i < parameterInfos.Length; i++)
            {
                var parameter = parameterInfos[i];

                if ((parameter.Name.Equals(nameof(IsServiceProvider), StringComparison.OrdinalIgnoreCase) || parameter.Name == "isServiceProvider") && IsServiceProvider.HasValue)
                {
                    if (parameters[i] is bool isServiceProviderValue && isServiceProviderValue != IsServiceProvider.Value)
                    {
                        throw new ArgumentException("Invalid service provider parameter value.");
                    }
                }

                if ((parameter.Name.Equals(nameof(IsDatabaseOptions), StringComparison.OrdinalIgnoreCase) || parameter.Name == "isDatabaseOptions") && IsDatabaseOptions.HasValue)
                {
                    if (parameters[i] is bool isDatabaseOptionsValue && isDatabaseOptionsValue != IsDatabaseOptions.Value)
                    {
                        throw new ArgumentException("Invalid database options parameter value.");
                    }
                }
            }
        }
    }

}
