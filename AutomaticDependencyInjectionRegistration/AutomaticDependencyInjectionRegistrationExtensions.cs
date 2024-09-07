using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Reflection;

namespace AutomaticDependencyInjectionRegistration
{
    /// <summary>
    /// Extensões para registro automático de serviços usando atributos personalizados.
    /// Esta classe fornece métodos para registrar serviços de injeção de dependência
    /// com base em um atributo personalizado, automatizando o processo de registro
    /// e reduzindo a necessidade de configuração manual.
    /// </summary>
    public static class AutomaticDependencyInjectionRegistrationExtensions
    {
        /// <summary>
        /// Cache para armazenar tipos de assemblies que já foram processados,
        /// prevenindo a necessidade de repetição de operações de reflexão.
        /// </summary>
        private static readonly ConcurrentDictionary<Assembly, Type[]> CachedTypesWithAttribute = new();

        /// <summary>
        /// Adiciona serviços ao <see cref="IServiceCollection"/> com base no atributo
        /// <see cref="RegisterServiceAttribute"/> encontrado nos tipos dos assemblies carregados.
        /// </summary>
        /// <param name="services">A coleção de serviços onde os serviços serão registrados.</param>
        /// <returns>A coleção de serviços atualizada.</returns>
        /// <exception cref="ArgumentNullException">Se <paramref name="services"/> for nulo.</exception>
        public static IServiceCollection AddServicesByAttribute(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services, nameof(services));

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            Parallel.ForEach(assemblies, assembly =>
            {
                var typesWithAttribute = CachedTypesWithAttribute.GetOrAdd(assembly, GetTypesWithAttribute);

                foreach (var classType in typesWithAttribute)
                {
                    RegisterService(services, classType);
                }
            });

            return services;
        }

        /// <summary>
        /// Obtém todos os tipos de um assembly que possuem o atributo
        /// <see cref="RegisterServiceAttribute"/>.
        /// </summary>
        /// <param name="assembly">O assembly do qual os tipos serão obtidos.</param>
        /// <returns>Uma matriz de tipos que possuem o atributo RegisterServiceAttribute.</returns>
        private static Type[] GetTypesWithAttribute(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes()
                    .Where(t => t.GetCustomAttribute<RegisterServiceAttribute>() is not null)
                    .ToArray();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types
                    .Where(t => t != null && t.GetCustomAttribute<RegisterServiceAttribute>() is not null)
                    .ToArray();
            }
        }

        /// <summary>
        /// Registra um tipo no <see cref="IServiceCollection"/> com base no atributo
        /// <see cref="RegisterServiceAttribute"/> encontrado no tipo.
        /// </summary>
        /// <param name="services">A coleção de serviços onde o serviço será registrado.</param>
        /// <param name="classType">O tipo a ser registrado.</param>
        private static void RegisterService(IServiceCollection services, Type classType)
        {
            var attribute = classType.GetCustomAttribute<RegisterServiceAttribute>();
            var interfaceType = attribute.InterfaceType ?? classType;

            if (!IsServiceRegistered(services, interfaceType, classType))
            {
                services.Add(new ServiceDescriptor(interfaceType, classType, attribute.Lifetime));
            }
        }

        /// <summary>
        /// Verifica se um serviço já está registrado no <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">A coleção de serviços onde o serviço será verificado.</param>
        /// <param name="interfaceType">O tipo da interface do serviço.</param>
        /// <param name="classType">O tipo da classe do serviço.</param>
        /// <returns>Verdadeiro se o serviço já estiver registrado; caso contrário, falso.</returns>
        private static bool IsServiceRegistered(IServiceCollection services, Type interfaceType, Type classType)
        {
            return services.Any(s => s.ServiceType == interfaceType && s.ImplementationType == classType);
        }
    }

    /// <summary>
    /// Atributo personalizado para registrar serviços na injeção de dependência.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class RegisterServiceAttribute : Attribute
    {
        /// <summary>
        /// Obtém o tempo de vida do serviço especificado no atributo.
        /// </summary>
        public ServiceLifetime Lifetime { get; }

        /// <summary>
        /// Obtém o tipo de interface especificado no atributo.
        /// </summary>
        public Type? InterfaceType { get; }

        /// <summary>
        /// Inicializa uma nova instância do atributo RegisterServiceAttribute com o tempo de vida especificado.
        /// </summary>
        /// <param name="lifetime">O tempo de vida do serviço.</param>
        /// <param name="interfaceType">O tipo de interface opcional.</param>
        public RegisterServiceAttribute(ServiceLifetime lifetime, Type? interfaceType = null)
        {
            Lifetime = lifetime;
            InterfaceType = interfaceType;
        }
    }
}