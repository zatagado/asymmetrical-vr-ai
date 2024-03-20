# Asymmetrical VR Game AI
This repository contains all of the AI code from my asymmetrical virtual reality (VR) game project I worked on in college, named *Eidolon*. This is a slice of the much larger code base that I wanted to show off on my portfolio. 

### Check out this [video](https://youtu.be/dneIoCeTpTM?si=D8zD-7PSECiqe2Cy) I posted to my portfolio YouTube channel for a basic gameplay and tech showcase.

## Gameplay Overview
***Eidolon*** is an asymmetrical multiplayer game where one player in VR faces against four players on mouse and keyboard (PC). Gameplay is asymmetrical not only because some players interface with VR devices and others with mouse and keyboard, but also players using different devices have opposing goals.

The ***Eidolon*** arena contains a **relic**, several **consoles**, and two **jails** scattered around. The **relic** is a red cube surrounded by red, green, and blue force field **barriers**. The **consoles** are small colored stands with buttons on top. **Consoles** can be colored red, green, blue, and white and will swap colors every 60 seconds.

At the beginning of the match, three of the PC players are assigned the colors red, green and blue, with the fourth being the **special** player. The PC players with assigned colors may only press buttons on **consoles** with their same color, but they are unable to see the colors of **consoles**. Only the **special** PC player can see the **console** colors, and it is their job to point out the **console** colors to the other PC players. 

The goal of the PC players is to win the game by getting to the **relic**. In order to get to the **relic**, they must first deactivate the colored **barriers** surrounding it. They can do this by pressing the buttons on the **consoles** with colors corresponding to a **barrier** color. An example gameplay sequence might go like this:

- The **special** PC player points out one of the red **consoles** to the red PC player. The red PC player presses the button on the red **console** and the red **barrier** around the **relic** is deactivated. 
- The **special** PC player points out green and blue **consoles** to the green and blue players, and those respective buttons are pressed, causing the green and blue **barriers** to also be deactivated.
- The PC players are able to reach the **relic** because all colored barriers were deactivated and the PC players win the game.

The goal of the VR player is to prevent the PC players from reaching the **relic**. They can do this by either sending all of the PC players to **jail** or by keeping them from reaching the **relic** until the game timer runs out. The VR player has several abilities at their disposal to assist their goal. They can tag PC players to send them to jail and they can shoot a laser beam that will send PC players to jail when they run into it. They can also throw a frisbee that they can teleport to.

## AI Behavior

### Behavior Trees