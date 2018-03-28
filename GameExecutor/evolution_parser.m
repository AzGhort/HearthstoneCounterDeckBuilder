clear;
fid = fopen('outcurve.txt');
tline = fgetl(fid);
x=1:50;
y=[];
err=[];
while ischar(tline)
    tline(tline==',') = '.';   
    C = strsplit(tline,' ');
    numbers = int64(str2double(C));
    max0 = int64(max(numbers));
    min0 = int64(min(numbers));
    avg = (max0 + min0)/2;
    y = [y, avg];
    err = [err, (max0-min0)/2];
    tline = fgetl(fid);
end
errorbar(x,y,err);
fclose(fid);