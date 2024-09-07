```md
# Automatic Dependency Injection Registration

A **AutomaticDependencyInjectionRegistration** é uma biblioteca que automatiza o processo de registro de serviços na injeção de dependência em projetos .NET, utilizando atributos personalizados. Isso reduz a necessidade de registrar manualmente cada serviço no `IServiceCollection`, simplificando o processo e tornando o código mais limpo e fácil de manter.

## Instalação

Você pode instalar o pacote diretamente do NuGet executando o seguinte comando no terminal:

```bash
dotnet add package AutomaticDependencyInjectionRegistration
```

Ou via Visual Studio:

1. Clique com o botão direito no seu projeto.
2. Selecione **Gerenciar Pacotes NuGet**.
3. Procure por `AutomaticDependencyInjectionRegistration` e clique em **Instalar**.

## Como Funciona

A biblioteca permite que você adicione um atributo personalizado `[RegisterService]` às suas classes, indicando o tempo de vida desejado (Transient, Scoped, Singleton) e a interface que a classe implementa (opcional). Em seguida, você pode chamar o método `AddServicesByAttribute` no `IServiceCollection` para registrar automaticamente todos os serviços.

### Atributo `RegisterService`

O atributo `RegisterServiceAttribute` é usado para marcar classes que devem ser registradas no container de injeção de dependências. Ele aceita dois parâmetros:

- **lifetime**: Define o tempo de vida do serviço (Transient, Scoped ou Singleton).
- **interfaceType** (opcional): Especifica a interface que a classe implementa. Se não for fornecido, a própria classe será registrada.

### Exemplo de uso

#### 1. Definir os serviços com o atributo `[RegisterService]`

Aqui está um exemplo de como marcar suas classes para registro automático:

```csharp
using AutomaticDependencyInjectionRegistration;
using Microsoft.Extensions.DependencyInjection;

// Define um serviço com tempo de vida Transient
[RegisterService(ServiceLifetime.Transient)]
public class MeuServico : IMeuServico
{
    public void Executar() => Console.WriteLine("Serviço executado!");
}

// Define outro serviço, associando a interface manualmente
[RegisterService(ServiceLifetime.Singleton, typeof(IMeuOutroServico))]
public class MeuOutroServico : IMeuOutroServico
{
    public void Processar() => Console.WriteLine("Outro serviço processado!");
}
```

#### 2. Registrar os serviços no `IServiceCollection`

Após marcar os serviços com o atributo, basta usar a extensão `AddServicesByAttribute` para registrar automaticamente todos os serviços marcados nos assemblies carregados:

```csharp
using AutomaticDependencyInjectionRegistration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Adiciona automaticamente todos os serviços marcados com [RegisterService]
        services.AddServicesByAttribute();
    })
    .Build();

host.Run();
```

Com isso, todos os serviços que possuem o atributo `[RegisterService]` serão automaticamente registrados na coleção de serviços.

#### 3. Consumir os serviços injetados

Agora você pode consumir os serviços registrados via injeção de dependência em qualquer classe do seu projeto:

```csharp
public class MeuCliente
{
    private readonly IMeuServico _meuServico;
    private readonly IMeuOutroServico _meuOutroServico;

    public MeuCliente(IMeuServico meuServico, IMeuOutroServico meuOutroServico)
    {
        _meuServico = meuServico;
        _meuOutroServico = meuOutroServico;
    }

    public void ExecutarServicos()
    {
        _meuServico.Executar();
        _meuOutroServico.Processar();
    }
}
```

### Cache de Tipos e Otimização

A biblioteca utiliza um cache (`ConcurrentDictionary`) para armazenar os tipos dos assemblies que já foram processados, evitando que a reflexão seja repetida desnecessariamente e melhorando a performance. Isso garante que cada assembly seja processado apenas uma vez durante a inicialização.

### Exceções Tratadas

Se algum tipo em um assembly não puder ser carregado (por exemplo, devido a erros de reflexão), a biblioteca trata essas exceções (`ReflectionTypeLoadException`) e apenas os tipos válidos são processados.

## Benefícios

- **Automação**: Elimina a necessidade de registrar serviços manualmente, simplificando a configuração da injeção de dependências.
- **Manutenção Reduzida**: Menos código para gerenciar, especialmente em grandes projetos.
- **Desempenho Otimizado**: Usa cache para evitar processamento redundante de assemblies.
- **Flexível**: Suporte a diferentes tempos de vida (Transient, Scoped, Singleton) e registro opcional de interfaces específicas.


## Licença

Este projeto é licenciado sob a licença MIT - consulte o arquivo [LICENSE](LICENSE) para mais detalhes.
```

### Pontos-Chave:
- Explicação sobre o propósito e funcionamento do atributo `RegisterServiceAttribute`.
- Exemplos claros de como usar a DLL no código.
- Instruções simples sobre como instalar e utilizar a biblioteca em aplicações .NET.
- Descrição de otimizações internas, como o uso de cache para melhorar a performance.
