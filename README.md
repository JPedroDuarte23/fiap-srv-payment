# FiapSrvPayment - API de Carrinho e Pagamento

## 📖 Sobre o Projeto

**FiapSrvPayment** é um microserviço em .NET 8 que gerencia o carrinho de compras e o processo de checkout da plataforma de jogos. Ele permite que os usuários (*Players*) adicionem, removam e visualizem jogos em seus carrinhos, além de finalizar a compra.

Seguindo o padrão de arquitetura limpa dos demais serviços da plataforma, o projeto é estruturado em camadas (Domain, Application, Infrastructure, API), garantindo um código desacoplado, testável e de fácil manutenção.

## ✨ Funcionalidades Principais

  - **Gerenciamento de Carrinho**: Operações completas para que o jogador possa adicionar e remover jogos do seu carrinho de compras.
  - **Visualização do Carrinho**: Endpoint para listar todos os jogos que o usuário possui no carrinho no momento.
  - **Processo de Checkout**: Funcionalidade para finalizar a compra. Ao realizar o checkout, os jogos no carrinho são movidos para a biblioteca do usuário, o carrinho é esvaziado, e o total da compra é calculado.
  - **Mensageria com AWS SNS**: Após um checkout bem-sucedido, um evento contendo os detalhes da compra (ID do usuário, email, jogos comprados, valor total) é publicado em um tópico do AWS Simple Notification Service (SNS).
  - **Segurança**: Todos os endpoints são protegidos e requerem um token JWT válido com o papel de `Player`.

## 🚀 Tecnologias Utilizadas

  - **.NET 8**: Framework principal para a construção da API.
  - **ASP.NET Core**: Para a criação da API RESTful.
  - **MongoDB**: Banco de dados NoSQL utilizado para persistir os dados dos usuários, incluindo seus carrinhos e bibliotecas de jogos.
  - **AWS (Amazon Web Services)**:
      - **SNS (Simple Notification Service)**: Para a publicação de eventos de checkout bem-sucedido, permitindo a comunicação assíncrona com outros serviços.
      - **Parameter Store**: Para o gerenciamento seguro de `secrets`, como a chave de assinatura JWT e a connection string do banco de dados em ambiente de produção.
      - **S3 (Simple Storage Service)**: Para persistir as chaves de criptografia do Data Protection entre as instâncias da aplicação.
      - **ECS (Elastic Container Service)**: Utilizado para a orquestração de contêineres e deploy da aplicação em produção.
  - **Docker**: Para a containerização do microserviço.
  - **Serilog**: Para logging estruturado, facilitando o monitoramento e a rastreabilidade das requisições.
  - **Swagger (OpenAPI)**: Para a documentação interativa da API.
  - **xUnit & Moq**: Para a escrita e execução de testes unitários.

## 🏗️ Arquitetura

O projeto adota uma arquitetura limpa, com uma separação clara de responsabilidades entre quatro camadas principais:

  - **`FiapSrvPayment.Domain`**: Camada mais interna, contendo as entidades de negócio (`User`, `Player`, `Game`, etc.) e enums. É o núcleo do domínio, sem dependências externas.
  - **`FiapSrvPayment.Application`**: Contém a lógica de negócio da aplicação. Define DTOs, interfaces de serviços (`ICartService`) e repositórios, e a implementação dos serviços que orquestram as operações do carrinho e checkout.
  - **`FiapSrvPayment.Infrastructure`**: Implementa as interfaces da camada de aplicação. É responsável pela comunicação com o banco de dados (repositórios MongoDB), middlewares (tratamento de exceções, Correlation ID) e integração com serviços da AWS.
  - **`FiapSrvPayment.API`**: Camada de apresentação que expõe os endpoints RESTful da API. Lida com as requisições HTTP, autenticação e autorização.

## ⚙️ CI/CD - Integração e Implantação Contínua

O projeto possui um pipeline de CI/CD robusto e automatizado utilizando **GitHub Actions**, que gerencia todo o ciclo de vida da aplicação.

1.  **Orquestrador (`ci-cd.yml`)**: Inicia o fluxo de trabalho em cada push ou merge na branch `main`.
2.  **CI (`ci.yml`)**:
      - Realiza o build da aplicação .NET.
      - Executa a suíte de testes unitários e gera relatórios de cobertura de código.
      - Envia os resultados para o **SonarCloud** para análise de qualidade e vulnerabilidades de código.
3.  **CD (`cd.yml`)**:
      - Após a conclusão bem-sucedida da etapa de CI, faz o login no Docker Hub.
      - Constrói a imagem Docker da aplicação.
      - Envia a imagem para o repositório do **Docker Hub**.
4.  **Deploy (`deploy-aws.yml`)**:
      - Com a nova imagem disponível, este workflow realiza o deploy no ambiente da **AWS**.
      - Ele atualiza a definição de tarefa (task definition) no **AWS ECS** com a nova imagem e implanta a versão mais recente do serviço de forma automatizada.

## Endpoints da API

Abaixo estão os endpoints disponíveis para o gerenciamento do carrinho.

### Cart (`/api/cart`)

  - `GET /`: Retorna os jogos presentes no carrinho do usuário autenticado.
  - `POST /?gameId={gameId}`: Adiciona um jogo ao carrinho do usuário.
  - `DELETE /?gameId={gameId}`: Remove um jogo do carrinho do usuário.
  - `POST /checkout`: Finaliza a compra, move os jogos do carrinho para a biblioteca e publica um evento de sucesso.

*Nota: Todos os endpoints requerem autenticação e o papel de `Player`.*

## 🏁 Como Executar Localmente

### Pré-requisitos

  - [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
  - [Docker Desktop](https://www.docker.com/products/docker-desktop)
  - Um editor de código de sua preferência (ex: VS Code, Visual Studio).

### 1\. Configuração do Ambiente

1.  **Clone o repositório:**

    ```bash
    git clone https://github.com/jpedroduarte23/fiap-srv-payment.git
    cd fiap-srv-payment
    ```

2.  **Inicie o MongoDB com Docker:**

    ```bash
    docker run -d -p 27017:27017 --name mongo mongo:latest
    ```

### 2\. Configuração da Aplicação

1.  **Configure a Connection String**:
    No arquivo `FiapSrvPayment.API/appsettings.Development.json`, certifique-se de que a connection string do MongoDB está configurada corretamente:

    ```json
    "ConnectionStrings": {
      "MongoDbConnection": "mongodb://localhost:27017/"
    }
    ```

2.  **Restaure as dependências e execute a aplicação**:
    Navegue até a pasta raiz do projeto e execute o seguinte comando:

    ```bash
    dotnet run --project FiapSrvPayment.API/FiapSrvPayment.API.csproj
    ```

3.  **Acesse a API**:
    A aplicação estará disponível em `https://localhost:7176` ou `http://localhost:5197`.
    A documentação do Swagger pode ser acessada através da URL `https://localhost:7176/swagger`.
