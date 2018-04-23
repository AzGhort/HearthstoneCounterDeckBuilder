clear;
fid = fopen('outcurve5.txt');
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
figure;
plot(x,y);
fclose(fid);