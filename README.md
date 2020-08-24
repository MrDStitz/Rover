# **Mars Rover Images Project**

## This application uses NASA's public api to request mars rover images for a given date. The images are stored locally and displayed in the browser output.

### The application is written in Microsoft C# on the .NET Core framework.

## To run the application in VisualStudio, simply open the solution and select the IIS Express runtime (F5)

### **Please note:**
### 1) To modify/add new dates to query, update the JSON string found in the DATES.TXT file located in the root of this project directory
### 2) The images returned from NASA are stored in the LocalImageStorage directory (also in the project directory root)

## To build and run the application using a Docker Image, run the following commands from PowerShell command prompt:
### 1) docker build -t rover .
### 2) docker run -it --rm -p 5000:80 --name rovercontainer rover
