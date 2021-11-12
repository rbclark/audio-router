AudioRouter
===========

This tools deploys an API which can be used to play arbitrary MP3 files on the host system and route them to different output devices. Its current purpose is to play different sounds from different locations in a haunted house simultaneously.

## Features
* Lists all audio devices connected to the host
* Asynchronously plays MP3 files on the host system (can play multiple files at the same time)

## Usage

### Listing audio devices
`localhost:8080/devices`

Returns a list of all audio devices connected to the host and their UUID.

### Playing MP3 files
`localhost:8080/play?device=<UUID>&file=<absolute or relative path to file>&volume=<0.0-1.0>`

Volume is optional and does not always work, depending on the audio device on your system.
