rm newdeck.txt
# cat deck*.txt > out.txt
rm deck*.txt
cp out.txt ~/bakal/HearthstoneAI/HillClimbStep/HillClimbStep/HillClimbStep/bin/Release/out.txt
cd ~/bakal/HearthstoneAI/HillClimbStep/HillClimbStep/HillClimbStep/bin/Release
mono HillClimbStep.exe
rm out.txt
cp newdeck.txt ~/bakal/HearthstoneAI/GameExecutor/GameExecutor/bin/Release/newdeck.txt
rm newdeck.txt
cd ~/bakal/HearthstoneAI/GameExecutor/GameExecutor/bin/Release