cat deck*.txt > out.txt
cp out.txt ~/bakal/HearthstoneAI/HillClimbStep/HillClimbStep/HillClimbStep/bin/Release/out.txt
cd ~/bakal/HearthstoneAI/HillClimbStep/HillClimbStep/HillClimbStep/bin/Release
mono HillClimbStep.exe
cp newdeck.txt ~/bakal/HearthstoneAI/GameExecutor/GameExecutor/bin/Release/newdeck.txt
cd ~/bakal/HearthstoneAI/GameExecutor/GameExecutor/bin/Release