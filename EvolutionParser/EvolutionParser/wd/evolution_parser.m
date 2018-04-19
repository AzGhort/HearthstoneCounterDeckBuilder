clear;
fid = fopen('outcurve.txt');
tline = fgetl(fid);
x=1:40;
y=[];
%y2 = [];
neg=[];
pos=[];
while ischar(tline)
    tline(tline==',') = '.';   
    C = strsplit(tline,'-');
    numbers = str2double(C);
    max = numbers(3);
    min = numbers(1);
    avg = numbers(2);
    %y2 = [y2, numbers(4)];
    y = [y, avg];
    neg = [neg, avg-min];
    pos = [pos, max-avg];
    tline = fgetl(fid);
end
errorbar(x,y,neg,pos);
%figure;
%plot(x, y2);
fclose(fid);