# Cupom Na Mão 🧾

**Conversor de XML NFC-e para DANFE PDF — Ferramenta de Suporte Técnico**

O **Cupom Na Mão** é uma aplicação desktop nativa do Windows, construída em C# (.NET 8 WPF), projetada especificamente para equipes de suporte técnico. Ela permite abrir rapidamente arquivos XML de NFC-e, visualizar um resumo inteligente e gerar instantaneamente o DANFE em PDF no formato cupom (80mm).

A aplicação funciona **100% offline**, não depende de navegadores e foca em alta produtividade (menos cliques).

---

## ✨ Funcionalidades

- **Abertura Rápida:** Suporte a arrastar e soltar (Drag & Drop) de arquivos XML diretamente na tela.
- **Resumo Inteligente:** Exibe nome do cliente, valor total, data, chave de acesso e lista de produtos antes mesmo de gerar o PDF.
- **Geração de PDF (80mm):** Layout otimizado para cupom, com geração automática de QR Code baseada nos dados do XML.
- **Processamento em Lote:** Selecione dezenas de XMLs de uma vez e gere todos os PDFs automaticamente em uma pasta.
- **Geração de ZIP:** Compacta múltiplos PDFs gerados em um único arquivo ZIP, pronto para envio ao cliente.
- **Histórico Recente:** Acesso rápido aos últimos XMLs abertos.
- **Ações Rápidas:** Copiar chave de acesso, imprimir, salvar PDF, consultar na SEFAZ.
- **Temas:** Suporte a tema Claro e Escuro.

---

## 🚀 Como Compilar (Build)

O projeto está configurado para ser compilado como um **Executável Único (.exe)**, sem necessidade de instalação complexa.

### Pré-requisitos
- Windows 10 ou superior
- [.NET 8 SDK instalado](https://dotnet.microsoft.com/download/dotnet/8.0)

### Opção 1: Usando os scripts inclusos (Recomendado)
1. Extraia o código-fonte em uma pasta.
2. Dê um duplo-clique no arquivo `build.bat` (ou execute `build.ps1` no PowerShell).
3. O script irá restaurar os pacotes e compilar o projeto em modo Release.
4. O executável final estará disponível na pasta `publish\CupomNaMao.exe`.

### Opção 2: Usando o Visual Studio 2022
1. Abra o arquivo `CupomNaMao.sln` no Visual Studio 2022.
2. Certifique-se de que o modo de compilação está em **Release**.
3. Clique com o botão direito no projeto `CupomNaMao` > **Publicar**.
4. Use o perfil de publicação local para gerar o executável único.

---

## 🛠️ Arquitetura do Projeto

O código foi estruturado em camadas para facilitar a manutenção e evolução:

- `/Models`: Estruturas de dados que representam a NFC-e (Emitente, Produtos, Pagamentos).
- `/Services`: Regras de negócio e processamento.
  - `XmlReaderService.cs`: Lê e valida o XML da NFC-e.
  - `DanfeGeneratorService.cs`: Desenha o PDF usando a biblioteca PdfSharp.
  - `QrCodeService.cs`: Gera o QR Code em imagem.
  - `PdfService.cs`: Gerencia salvamento, impressão, lotes e ZIP.
  - `HistoricoService.cs` e `ConfiguracaoService.cs`: Gerenciam dados locais do usuário.
- `/UI`: Interface do usuário em WPF.
  - `/Views`: Janelas XAML (MainWindow, ConfiguracoesWindow).
  - `/ViewModels`: Lógica de apresentação e controle de estado (MVVM básico).
  - `/Themes`: Dicionários de recursos para cores e estilos (Claro/Escuro).

---

## 📦 Dependências (NuGet)

O projeto utiliza bibliotecas consolidadas e gratuitas:
- **PdfSharp** (v6.1.1): Geração nativa de PDFs de alta performance.
- **QRCoder** (v1.6.0): Geração de códigos QR 100% em C#.

---

## 🧪 Testando

Na pasta `exemplos/`, há um arquivo `exemplo_nfce.xml` que você pode arrastar para dentro da aplicação para ver como ela funciona na prática.

---
*Desenvolvido por Manus AI - 2025*
