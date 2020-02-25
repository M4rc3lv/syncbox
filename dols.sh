#!/usr/bin/env bash
cd "$1"

for f in *; do
 [[ -e $f ]] || continue
 if [[ -f $f ]]; then
  size=$(wc -c < "$f")
  type='f'
 else
  size=0
  type='d'
 fi
 d=$(date -r "$f" "+%d-%m-%Y %H:%M:%S")
 
 #d=$(date '+%d-%m-%Y %H:%M:%S')

 #Format is: f///2046///filenaam.txt///date time of d///2046///diretoryname.txt///datumtijd
 echo $type"///"$size"///"$f"///"$d

done

exit 0
