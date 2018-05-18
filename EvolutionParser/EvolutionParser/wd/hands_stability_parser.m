
[x1, y1] = getXY('fixed hands.txt');
[x2, y2] = getXY('random hands.txt');

figure;
plot(x1,y1, '-r', 'LineWidth',2);
set(gca,'fontsize',20);
xlabel('Testy');
ylabel('Procentu�ln� �sp�nost');
title('Stabilita test� bal��ku BasicPriest');
hold on
plot(x2,y2,'--b','LineWidth',2);
legend('Pou�it� 3 fixn�ch rozd�n� do �vodn�ch rukou pro ka�d� meta-bal��ek, rozptyl: 4.14', 'Pou�it� n�hodn�ch rozd�n� do �vodn�ch rukou pro ka�d� meta-bal��ek, rozptyl: 4.17');
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