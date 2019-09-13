# WARNING
Even though this is one of my old personal projects, it is still in the early phases of development.
Not stable nor production ready.
# Introduction
A RAT application built in CSharp.

# Features
- Console Based(For now, in the future it will have UI)
- Process List
- Explore directories
- Desktop streaming
- Reverse Shell
- Get client's system information

# TODO
- Encryption
- Before changing directory, I have to implement directory exists checking, I should be using
PacketFileSystem.FileSystemFocus.DirectoryExists enumerator
- Review the thread safety, mainly in the Ui, When we press Ctrl + C the callback is called in
another thread, and in that callback I make some changes to properties, is my implementation thread safe?
- Ctr + C callback is only triggered once, fix it.
- Camera Streaming
- Graphical User Interface
