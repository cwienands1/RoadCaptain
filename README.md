# RoadCaptain

RoadCaptain is an app that makes riding on Zwift even more fun and can really push your limits in Watopia. 

> Can't wait? Download RoadCaptain [right here](https://github.com/sandermvanvliet/RoadCaptain/releases/download/v0.2.0.0/RoadCaptain.Installer_0.2.0.0.msi)

How? Simple: you are no longer limited to the fixed routes in Watopia, with RoadCaptain you can build your own routes and explore Watopia even more.

Always wanted to do 3 laps on the Volcano as a warm up followed by blasting through the Jungle Loop? Now you can!

Of course, you can already _sort of_ do this by starting a free ride in Zwift and using the turn buttons in the game but when you're powering through those segments it's super easy to miss the turn and that's not great for your flow right?

RoadCaptain takes away all the hassle of having to keep paying attention to upcoming turns and remembering which ones to take to follow the route you want. 

So how does RoadCaptain make that work?

When you start RoadCaptain it will connect to Zwift and receive position updates and upcoming turns when you are riding. Your current position is matched against the route you've designed so RoadCaptain knows which turn to take next. When you are getting close to a turn (when you go past the turn marker on the side of the road), RoadCaptain tells Zwift which turn to take.

Sounds simple right? It also means you don't have to keep thinking about which way to go and can concentrate on pushing the power to the pedals!

## How to use RoadCaptain

RoadCaptain is actually two apps:

- The route builder
- The runner

When you install RoadCaptain, both apps will be installed and you can find them in your start menu.

>**NOTE:** Currently you'll see a warning that the installer is from an untrusted developer. That's to be expected, the installer hasn't been signed with a proper certificate yet.

First, let's take a look at how to build a route you can ride.

When you open the RoadCaptain Route Builder you'll see this screen:
![RouteBuilder-step-1](/images/RouteBuilder-step-1.png)

The green segments are the starting points you can use and they are the same starting points as the regular Zwift routes.

When you click a starting segment (I've used one close to the desert here), you can see that it's now highlighted in yellow and it appears as the first segment in the list on the left of the screen:
![RouteBuilder-step-2](/images/RouteBuilder-step-2.png)

From there you can start clicking segments to build your route, here I've selected a warm up through the desert flats and then up into the big foot hills for some climb work:
![RouteBuilder-step-3](/images/RouteBuilder-step-3.png)

On the left side of the screen you can see all the segments of the route as well as the total length and the ascent and descent you'll encounter when you are going to ride it.

Now hit the save button (yes, the floppy disk! 😁) and pick a file name and a location to store it.

Congrats! Your first route design is now done!

>**Pro tip:** when you click the play icon, RoadCaptain will simulate the route so that you can easily see how it will run 👍 This is especially handy if you cross a segment multiple time, for example when you have a few loops.

## Riding a route with RoadCaptain

Now that you've designed your first route it's time to kit up, fire up Zwift and get rolling 💪

From the start menu click _RoadCaptain Runner_, that will show you this window:
![Runner-auth-step-1](/images/Runner-auth-step-1.png)

When you click _Log in_ a browser window will be opened where you can log in to the Zwift website so that RoadCaptain can authenticate with Zwift. RoadCaptain does not have access to your username/password only a token generated by the Zwift website after you log in.
![Runner-auth-step-2](/images/Runner-auth-step-2.png)

After you have logged in the screen will show your Zwift picture and name in the game:
![Runner-auth-step-3](/images/Runner-auth-step-3.png)

Next click the _Select_ button and find the route you've stored before.

Your screen should now look like this:
![Runner-step-2](/images/Runner-step-2.png)

Now click _Let's go!_

If you haven't started Zwift yet or if you are not yet on the starting segment you will see this screen:
![Runner-step-3](/images/Runner-step-3.png)

Once you are in the game and on the right route RoadCaptain gets your position from Zwift and will change to the in-game window:
![Runner-step-4](/images/Runner-step-4.png)

Here you'll see a _lot_ of details on your route. Let's take a look, we've got:

- Elapsed distance on route vs total distance
- Elapsed ascent vs total ascent
- Elapsed descent vs total descent
- A progress bar with your progress on the route (the orange one)
- The current segment with:
    - The next turn
    - Length of the segment
    - Ascent on the segment
    - Descent on the segment
    - Progress bar with your progress on the current segemnt (the blue one)
- The next segment with:
    - The next turn
    - Length of the segment
    - Ascent on the segment
    - Descent on the segment

As you can see, there is plenty of information so that you can see what's coming up and you won't be surprised by that monster climb just around the corner!

## Requirements and installing

RoadCaptain requires a Windows PC with .Net 5 installed (which you can download [from here](https://dotnet.microsoft.com/en-us/download/dotnet/5.0)).

I've tested RoadCaptain with Zwift running on the same machine. _However_, as long as you are on the same network you can use Zwift on an iPad or Apple TV as long as you have a PC nearby where you can start the RoadCaptain Runner app.

The RoadCaptain installer can be found here [RoadCaptain.Installer_0.2.0.0.msi](https://github.com/sandermvanvliet/RoadCaptain/releases/download/v0.2.0.0/RoadCaptain.Installer_0.2.0.0.msi)

When you start the RoadCaptain Runner and click _Let's go!_ for the first time, Windows will ask you to allow network traffic on the private network. This is expected and is required for RoadCaptain to be able to talk to Zwift. If you accidentally click _deny_ you will need to uninstall and re-install RoadCaptain for this dialog to show again.

Before running Zwift I would recommend that you change Zwift to use windowed instead of full-screen mode. To change this go to the Zwift settings file (`prefs.xml`) (should be in your My Documents\zwift folder). Open that file and change `<FULLSCREEN>1</FULLSCREEN>` to `<FULLSCREEN>0</FULLSCREEN>`, save and close the file.

## Notes on testing

This is a beta version of RoadCaptain so expect quite a few rough edges and bugs. 

Some known issues:

- Currently you can't use Zwift Companion and RoadCaptain at the same time.
- If you enter the wrong Zwift username and/or password RoadCaptain won't tell you. If RoadCaptain is stuck on the "Start Zwift" message you might want to double check your password and restart RoadCaptain.
- Every so often RoadCaptain remains stuck on the "Start Zwift" message when you are actually on the starting segment in Zwift. Either give it a minute or restart the RoadCaptain Runner
- The in-game screen most likely won't show up when running Zwift in full-screen mode. (see above in the installing section)

If you want to report a bug or provide other feedback, feel free to send me an email at [zwift@codenizer.nl](mailto:zwift@codenizer.nl)

RoadCaptain generates log files, you can find those in the installation directory. Please send them along (or the latest one) when you report a bug. That will help me diagnose the problem a lot quicker 👍

## Last but not least

Please note that RoadCaptain or myself are not associated with Zwift and the app has been built purely as an interesting experiment to see if I could do it. 

And yes, the screens do look a bit like Zwift but I hope I've made them just "off" enough to make it clear that it isn't Zwift itself.
