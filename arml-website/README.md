# ARML SDK: Documentation Website
**[GitHub](https://github.com/fubilab/arml-sdk) ·
  [Documentation](https://fubilab.github.io/arml-sdk/) ·
  [Project Home](https://emil-xr.eu/lighthouse-projects/upf-ar-magic-lantern/) ·
  [Discord](https://discord.gg/zWZT3yKf4q)**
<hr size="1" />

The ARML Documentation Website is build using [docfx](https://github.com/dotnet/docfx), a community-supported project.

## Getting Started

1. [Download .Net](https://dotnet.microsoft.com/en-us/download) and install it

2. Install docfx as a global tool:

    ```bash
    dotnet tool install -g docfx
    ```

3. [Download NodeJS](https://nodejs.org/en/) and install it

4. Install Node dependencies.  
   From the `arml-website` directory, run:

   ```bash
   npm install
   ``` 

5. Build docfx.  
   From the `arml-website` directory, run:
   
   ```bash
   docfx build
   ``` 


## Editing documentation

Once you have installed dependencies and done the initial `docfx build` described above, you can launch the docs site locally.

From the `arml-website` directory, run:

```bash
npm run docfx
```

Open a browser and load: http://localhost:8080

- When you make changes to files in the [/docs](./docs/) directory, the site will be rebuilt automatically. You will have to refresh the browser to see updates.

- Images must go in [/docs/images](./docs/images/) to be served

## Generating API docs

The [API docs](http://localhost:8080/api/) will not be found until you generate them. 

You can use docfx to generate them from the Unity project by running:

```bash
docfx metadata
docfx build
```

You should re-run this whenever you make changes to the Unity project source that might affect the documention.


<hr size="1">
<a href="https://www.upf.edu/web/fubintlab">
<img src="../arml-website/docs/images/FubIntLab.jpg" height="50" margin="5"/></a>
&nbsp;&nbsp;
<a href="https://emil-xr.eu">
<img src="../arml-website/docs/images/emil-logo.png" height="50"/></a>
&nbsp;&nbsp;
<a href="https://upf.edu">
<img src="../arml-website/docs/images/UPF.png" height="50"/></a>
<hr size="1">
<img src="../arml-website/docs/images/funded-by-the-eu.png" height="50" />