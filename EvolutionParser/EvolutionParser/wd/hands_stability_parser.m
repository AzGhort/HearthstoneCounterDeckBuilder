
[x1, y1] = getXY('fixed hands.txt');
[x2, y2] = getXY('random hands.txt');

figure;
plot(x1,y1, '-r', 'LineWidth',2);
set(gca,'fontsize',20);
xlabel('Testy');
ylabel('Procentuální úspìšnost');
title('Stabilita testù balíèku BasicPriest');
hold on
plot(x2,y2,'--b','LineWidth',2);
legend('Použití 3 fixních rozdání do úvodních rukou pro každý meta-balíèek, rozptyl: 4.14', 'Použití náhodných rozdání do úvodních rukou pro každý meta-balíèek, rozptyl: 4.17');
hold off


function [x, y] = getXY(filename)
fid = fopen(filename);
tline = fgetl(fid);
x=1:1001;
y=[];
tline(tline==',') = '.';   
C = strsplit(tline,' ');
numbers = str2double(C);
num = sort(numbers);
for (i = 1: length(num))
    y = [y, num(i)];
end
fclose(fid);
end