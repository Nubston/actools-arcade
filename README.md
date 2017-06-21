# CM’s Libraries Usage Example (Arcade Corsa, Console Wrapper)

Some examples of how CM’s libraries can be used to create some separate apps. Sadly, the whole integration process isn’t
very smooth or nice, but I believe this is still more or less usable.

- ### [Arcade Corsa](https://github.com/gro-ove/actools-arcade/tree/master/Arcade%20Corsa)
    Small WPF app with integrated DX11 renderer. Apparently, [WPF doesn’t work well](http://stackoverflow.com/a/6003900/4267982)
    with this sort of stuff, but it was worth a try.

    Offers some sort of gameplay by allowing a player to set new laptimes on Magione and then adding them to the table.
    Nothing serious.

    Other features:

    - Custom renderer (I’m going to copy it to `AcTools.Render` later, but it’ll keep being here as well an example of 
    extending original renderer);

    - Player can select custom colors;

    - Switching cars’ models on-fly.
    
    [![Arcade Corsa](http://i.imgur.com/or2Ft2g.png)](http://i.imgur.com/or2Ft2g.png)
    
- ### [ConsoleWrapper](https://github.com/gro-ove/actools-arcade/tree/master/ConsoleWrapper)
    Few hundred lines example of using CM libraries in console application. 

    <details> 
    <summary>You still would need to reference `FirstFloor.ModernUI`
    because it provides storage, logging, errors handling and stuff. I’ll try to sort this out later, but for now, please,
    consider this library as some sort of common basis for all Windows apps I use. 😬</summary>
    *How are you supposed to code C# apps anyway? If I would do it properly, I would have now like, hundreds of smallest assemblies, and app would weights twice as much just because of their metadata.*
    </details>
    
    [![ConsoleWrapper](http://i.imgur.com/Vv7teOF.png)](http://i.imgur.com/Vv7teOF.png)

# Build notes

 - Some of references were added directly as DLL-files. I didn’t use Nuget for all of references because eigher I needed specific libraries locations (this way, I’m able to use 32-bit libraries for 32-bit build and otherwise using simplest .csproj “trick”) or I slightly modified some of them (for example, now JSON-parser works reads numbers starting with “0” according to JSON specifications). All these references are located in Library directory, you can get them [here](https://drive.google.com/file/d/0B6GfX1zRa8pOMjdKTnZ1eDZ3SHc/view?usp=drivesdk). Or just collect them from scratch, those changes aren’t be very important.

 - You might need to install DirectX SDK to rebuild [Arcade Corsa/Render/Shaders/ShadersTemplate.tt](https://github.com/gro-ove/actools/blob/master/Arcade Corsa/Render/Shaders/ShadersTemplate.tt). But, just in case, builded *ShadersTemplate.cs* and *Shaders.resources* are already included.

 - Please, feel free to [contact me](https://trello.com/c/w5xT6ssZ/49-contacts) anytime. I don’t have any experience it open-source, there might be some things I forgot to mention.
