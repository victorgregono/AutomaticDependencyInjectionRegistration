
---

# Documentação de `AutomaticDependencyInjectionRegistration`

## Introdução

A DLL `AutomaticDependencyInjectionRegistration` automatiza o registro de serviços de injeção de dependência em aplicações C# usando atributos personalizados. Esta abordagem simplifica a configuração de serviços e reduz a necessidade de registros manuais.

## Instalação

Para utilizar a `AutomaticDependencyInjectionRegistration`, siga estes passos:

1. **Adicionar a DLL ao Projeto**

   Adicione a referência à DLL `AutomaticDependencyInjectionRegistration` no seu projeto. Isso pode ser feito através do NuGet ou adicionando uma referência de projeto diretamente.

## Código da DLL

Aqui está o código completo da DLL `AutomaticDependencyInjectionRegistration`:

```csharp
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

            // Obtém todos os assemblies carregados no domínio atual
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            // Processa cada assembly em paralelo para encontrar e registrar tipos com o atributo
            Parallel.ForEach(assemblies, assembly =>
            {
                // Obtém tipos com o atributo RegisterServiceAttribute do assembly atual
                var typesWithAttribute = CachedTypesWithAttribute.GetOrAdd(assembly, GetTypesWithAttribute);

                // Registra cada tipo encontrado na coleção de serviços
                foreach (var type in typesWithAttribute)
                {
                    RegisterService(services, type);
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
            return assembly.GetTypes()
                .Where(t => t.GetCustomAttribute<RegisterServiceAttribute>() is not null)
                .ToArray();
        }

        /// <summary>
        /// Registra um tipo no <see cref="IServiceCollection"/> com base no atributo
        /// <see cref="RegisterServiceAttribute"/> encontrado no tipo.
        /// </summary>
        /// <param name="services">A coleção de serviços onde o serviço será registrado.</param>
        /// <param name="type">O tipo a ser registrado.</param>
        private static void RegisterService(IServiceCollection services, Type type)
        {
            // Obtém o atributo RegisterServiceAttribute do tipo
            var attribute = type.GetCustomAttribute<RegisterServiceAttribute>();

            // Obtém todas as interfaces implementadas pelo tipo
            var interfaces = type.GetInterfaces();

            // Lógica para encontrar a interface correspondente com base no nome do tipo
            var serviceType = interfaces.FirstOrDefault(i => i.Name == $"I{type.Name}") ?? type;

            // Adiciona o serviço ao IServiceCollection com o tempo de vida especificado no atributo
            services.Add(new ServiceDescriptor(serviceType, type, attribute.Lifetime));
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
        /// Inicializa uma nova instância do atributo RegisterServiceAttribute com o tempo de vida especificado.
        /// </summary>
        /// <param name="lifetime">O tempo de vida do serviço.</param>
        public RegisterServiceAttribute(ServiceLifetime lifetime)
        {
            Lifetime = lifetime;
        }
    }
}
```

## Configuração do Projeto

### Configuração do `Startup`

Modifique a classe `Startup` do seu projeto para usar a DLL e registrar serviços automaticamente:

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Adiciona serviços automaticamente usando atributos personalizados
        services.AddServicesByAttribute();

        // Outros registros de serviços
        services.AddControllers();
    }

    // Método Configure não é mostrado aqui, mas você o usaria para configurar o pipeline de requisição
}
```

## Definição de Serviços

Os serviços devem ser marcados com o atributo `RegisterServiceAttribute`. Este atributo define o tempo de vida do serviço (por exemplo, Singleton, Scoped ou Transient).

### Exemplo 1: Classe com Interface

```csharp
[RegisterService(ServiceLifetime.Singleton)]
public class EmailService : IEmailService
{
    public void SendEmail(string recipient, string subject, string body)
    {
        // Implementação do envio de e-mail
        Console.WriteLine($"Email enviado para {recipient} com assunto {subject}.");
    }
}

public interface IEmailService
{
    void SendEmail(string recipient, string subject, string body);
}
```

### Exemplo 2: Classe Sem Interface

```csharp
[RegisterService(ServiceLifetime.Scoped)]
public class NotificationService
{
    public void Notify(string message)
    {
        // Implementação da notificação
        Console.WriteLine($"Notificação: {message}");
    }
}
```

## Uso dos Serviços

Depois que os serviços estiverem registrados, você pode injetá-los em seus controladores ou outras classes:

```csharp
public class HomeController : Controller
{
    private readonly IEmailService _emailService;
    private readonly NotificationService _notificationService;

    public HomeController(IEmailService emailService, NotificationService notificationService)
    {
        _emailService = emailService;
        _notificationService = notificationService;
    }

    public IActionResult Index()
    {
        _emailService.SendEmail("user@example.com", "Assunto", "Corpo do email");
        _notificationService.Notify("Notificação recebida.");

        return View();
    }
}
```

## Explicação do Código

### `AutomaticDependencyInjectionRegistrationExtensions`

- **`AddServicesByAttribute`**: Estende o `IServiceCollection` para adicionar serviços com base no atributo `RegisterServiceAttribute`. Percorre todos os assemblies carregados, encontra tipos com o atributo e os registra.

- **`GetTypesWithAttribute`**: Obtém todos os tipos de um assembly que possuem o atributo `RegisterServiceAttribute`.

- **`RegisterService`**: Registra um tipo no `IServiceCollection` usando o tempo de vida especificado no atributo. Se o tipo tiver uma interface correspondente, essa interface será usada para o registro.

### `RegisterServiceAttribute`

- **Propriedade `Lifetime`**: Define o tempo de vida do serviço (`ServiceLifetime.Singleton`, `ServiceLifetime.Scoped`, `ServiceLifetime.Transient`).

- **Construtor**: Inicializa o atributo com o tempo de vida especificado.

## Considerações Finais

- **Flexibilidade**: A DLL facilita a manutenção e escalabilidade do projeto, automatizando o registro de serviços e garantindo consistência.
- **Interface Opcional**: O registro de serviços não requer uma interface; serviços sem interfaces também podem ser registrados e injetados.

Com essa abordagem, você pode simplificar a configuração de injeção de dependência e focar no desenvolvimento das funcionalidades da sua aplicação.

--- 

Esta documentação fornece uma visão geral completa sobre como usar a DLL `AutomaticDependencyInjectionRegistration` em projetos C#. 
