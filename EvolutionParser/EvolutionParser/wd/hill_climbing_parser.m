[x, y, neg, pos] = getXY('outcurve hunter.txt');
[x1, y1, neg1, pos1] = getXY('outcurve real hunter.txt');

errorbar(x,y,neg,pos, '-b', 'LineWidth',2);
axis([0 40 0 100]);
set(gca,'fontsize',20);
xlabel('Iterace hill-climbingu');
ylabel('Procentu�ln� �sp�nost');
title('Procentu�ln� �sp�nost vyv�jen�ho bal��ku BasicHunter v pr�b�hu hill-climbingu');
hold on;
plot(x1,y1,'--r','LineWidth',2)
legend({'�sp�nost vyv�jen�ch bal��k� (nejlep��, medi�n, nejhor�� bal��ek v r�mci iterace)', 'Skute�n� pr�m�rn� �sp�nost nejlep��ho bal��ku ka�d� iterace'}, 'Location','southeast');
hold off;

figure;
[x, y, neg, pos] = getXY('outcurve priest.txt');
[x1, y1, neg1, pos1] = getXY('outcurve real priest.txt');

errorbar(x,y,neg,pos, '-b', 'LineWidth',2);
axis([0 40 0 100]);
set(gca,'fontsize',20);
xlabel('Iterace hill-climbingu');
ylabel('Procentu�ln� �sp�nost');
title('Procentu�ln� �sp�nost vyv�jen�ho bal��ku BasicPriest v pr�b�hu hill-climbingu');
hold on;
plot(x1,y1,'--r','LineWidth',2)
legend({'�sp�nost vyv�jen�ch bal��k� (nejlep��, medi�n, nejhor�� bal��ek v r�mci iterace)', 'Skute�n� pr�m�rn� �sp�nost nejlep��ho bal��ku ka�d� iterace'}, 'Location','southeast');
hold off;

function [x, y, neg, pos] = getXY(filename)
fid = fopen(filename);
tline = fgetl(fid);
x=1:40;
y=[];
neg=[];
pos=[];
while ischar(tline)
    tline(tline==',') = '.';   
    C = strsplit(tline,'-');
    numbers = str2double(C);
    max = numbers(3);
    min = numbers(1);
    avg = numbers(2);
    y = [y, avg];
    neg = [neg, avg-min];
    pos = [pos, max-avg];
    tline = fgetl(fid);
end
fclose(fid);
end