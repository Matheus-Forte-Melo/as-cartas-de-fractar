# Como clonar o repositório e abrir o projeto Unity no Unity Hub

## Pré-requisitos
- Git instalado
- Unity Hub instalado
- Unity Editor instalado (versão compatível com o projeto)

---

## 1) Clonar o repositório do GitHub

1. Copie a URL do repositório no GitHub  
   Exemplo: https://github.com/usuario/repositorio.git

2. Abra o terminal (ou Prompt de Comando / PowerShell)

3. Vá até a pasta onde deseja salvar o projeto:
   ```bash
   cd caminho/para/sua/pasta
   ```

4. Clone o repositório:
   ```bash
   git clone https://github.com/usuario/repositorio.git
   ```

5. Entre na pasta do projeto:
   ```bash
   cd repositorio
   ```

---

## 2) Adicionar o projeto no Unity Hub

1. Abra o Unity Hub
2. Vá na aba "Projects"
3. Clique em "Add" (ou "Open")
4. Selecione a pasta raiz do projeto clonado  
   (a pasta que contém: Assets, Packages e ProjectSettings)
5. Confirme para adicionar o projeto

---

## 3) Instalar a versão correta do Unity

1. Dentro da pasta do projeto, abra o arquivo:
   ProjectSettings/ProjectVersion.txt

2. Copie a versão do Unity indicada (exemplo: 2022.3.xf1)

3. No Unity Hub:
   - Vá em "Installs"
   - Clique em "Install Editor"
   - Instale a mesma versão encontrada no arquivo

---

## 4) Abrir o projeto no Unity

1. No Unity Hub, vá em "Projects"
2. Localize o projeto na lista
3. Selecione a versão correta do Unity (se solicitado)
4. Clique no projeto para abrir
5. Aguarde a primeira importação dos assets (pode demorar alguns minutos)

---

## Estrutura esperada do projeto Unity

A pasta selecionada deve conter:
- Assets/
- Packages/
- ProjectSettings/

Se essas pastas não existirem, você provavelmente selecionou a pasta errada.
