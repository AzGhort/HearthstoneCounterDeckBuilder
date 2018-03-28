clear;
fid = fopen('outcurve.txt');
tline = fgetl(fid);
x=1:1001;
y=[];
tline(tline==',') = '.';   
C = strsplit(tline,' ');
numbers = int64(str2double(C));
for (i = 1: length(numbers))
    y = [y, numbers(i)];
end
figure;
plot(x,y);
fclose(fid);