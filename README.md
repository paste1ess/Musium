# Musium
<img width="200" height="200" alt="logo" src="https://github.com/user-attachments/assets/cbd70dc1-2618-4517-9700-d4283dc7a622" />

a native WinUI 3 music player made in C#

<img width="1079" height="653" alt="{2082FA87-9E80-431C-AB4F-7EF18FE6CA30}" src="https://github.com/user-attachments/assets/8d9d57f4-30aa-4464-b94d-5317aba3e420" />
<img width="1079" height="653" alt="{948954F5-E65D-414E-AD63-C072C50EBBAA}" src="https://github.com/user-attachments/assets/a2e1bcfc-4d13-47b4-ab83-fb421ac849d9" />
<img width="1079" height="653" alt="{9254E485-7A20-438B-95FB-BE2482C7C179}" src="https://github.com/user-attachments/assets/222c9609-e6d6-4316-89ac-bf650b214140" />
<img width="1079" height="653" alt="{02AD702C-E1F8-4131-9438-8EEEC06966AB}" src="https://github.com/user-attachments/assets/de094d33-8fa7-4cc1-94f8-30e23fc6d0c9" />

## credits
logo by [GattoDev](https://github.com/GattoDev-debug)

## install instructions
go to releases, download the .msix for your cpu and install it

## build instructions
heads up, if you are planning on contributing, the dev branch is where things can change and PRs can be made. the main branch is reserved for stable builds only

- step 1: you need Visual Studio 2026 to build. make sure to install that, as older Visual Studio versions are not guarenteed to work and could break the project.
- step 2: open up the project, and right click it in the Solution Explorer.
- step 3: go to Package and Publish > Create App Packages
- step 4: choose Sideloading, make sure Enable automatic updates is **disabled**.
- step 5: go through it, mak sure Generate app bundle is set to Always or If needed.
- step 6: click create and install it from the destination folder
this is to build a release build. you can just test it as normal in Visual Studio if you are working on it
