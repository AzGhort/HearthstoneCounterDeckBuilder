
[x1, y1] = getXY('81games.txt');
[x2, y2] = getXY('135games.txt');
[x3, y3] = getXY('189games.txt');

figure;
plot(x1,y1, '-r', 'LineWidth',2);
set(gca,'fontsize',20);
xlabel('Testy');
ylabel('Procentuální úspìšnost');
title('Procentuální úspìšnost balíèku BasicHunter v testech');
hold on
plot(x2,y2,'--g','LineWidth',2);
plot(x3,y3,'-.b','LineWidth',2);
legend('81 her na test, rozptyl: 5.30', '135 her na test, rozptyl: 3.93', '189 her na test, rozptyl: 3.24');
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