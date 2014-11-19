Reuseable analysis library and Visual Studio extension with hundreds of open source refactorings and code issue fixes.

[Twitter](https://twitter.com/sharpdevelop) - [StackOverflow](http://stackoverflow.com/questions/tagged/nrefactory) - [Wiki](https://github.com/icsharpcode/NRefactory/wiki)

## What is NRefactory exactly?

NRefactory is a library geared towards anyone that needs IDE services - think code completion, refactoring, code fixes and more. It is being used by IDE projects such as [SharpDevelop](https://github.com/icsharpcode/SharpDevelop) and [MonoDevelop](https://github.com/mono/monodevelop), or [OmniSharp](https://github.com/OmniSharp) that provides IDE-like features in various cross platform editors.

## But I only care about Visual Studio!

We got you covered - NR6 is based on Roslyn, which means our refactorings and code issue fixes work there too. We even have a special project for that - the NR6 Pack.

## Tell me more about NR6 Pack Refactorings

You can get it [from the Visual Studio gallery](https://visualstudiogallery.msdn.microsoft.com/68c1575b-e0bf-420d-a94b-1b0f4bcdcbcc) via Tools/Extensions in Visual Studio (2015). It bundles a subset of the refactorings and code issue fixes.

## Wait, I don't get everything in VS?!

Yes, that is intentional. Do you need a second rename refactoring when one is already in-box with VS? We don't think so. Like stated earlier, we provide IDE services for a wide range of applications, and we cherry picked the things that make sense.

## Cool, how can I help?

Got a great idea for a refactoring or code issue fix? Either create an issue as a suggestion, or even better, contribute the actual code via a pull request.

## I like the idea of contributing, can you tell me where to put my new refactoring/code issue?

There are two solutions in the project. One is for the full stack of IDE services, and it is cross platform: NRefactory. The NR6 Pack for Visual Studio is separate from that.

## Why are the solutions separated?

To build the NR6 Pack, you need the Visual Studio SDK. We didn't want to burden the x-plat development with special casing those projects. And we are mostly linking files anyways.

## Linking files? Why not link the x-plat refactoring project?

We wanted to reduce the dependencies (and remember the part about cherry picking refactorings?) and thus the size of the vsix package. Oh, and it helps performance too.

## Ok, understood. So where do I put my new stuff then?

The source files go into ICSharpCode.NRefactory.CSharp.Refactoring. Then add a link to this source file in the respective folder of the NR6Pack.Refactorings project. Presto. This way your contribution is visible to the x-plat downstream projects as well as VS.

## You are returning weird derived CodeActions...

They serve the purpose of passing additional information along to IDEs and consumers other than VS. Please stick with this pattern. (Quick details: those pass along severity and spans, and allow link / insert mode in IDEs - see the [Wiki article](https://github.com/icsharpcode/NRefactory/wiki/CodeAction-Wrapper-Classes) for details)

## How do I get in touch with you?

Use GitHub issues and pull requests to your hearts content. Or go to our [SharpDevelop Twitter feed](https://twitter.com/sharpdevelop).

## Got a bit of history for me?

NRefactory is not exactly a new kid on the block. It started a very long time in SharpDevelop, was at times developed in parallel in MonoDevelop, and roundabout 2010 the two teams joined forces to bring NRefactory5 to life as a full open source stack of IDE services (MonoDevelop 3 and SharpDevelop 5 ship(ped) with this version). NR6 is leveraging Roslyn instead of homegrown stack components to make development easier and faster. After all, open source isn't about duplicating effort - at least in our book.

## The license?

MIT. Doesn't get any simpler than that.
