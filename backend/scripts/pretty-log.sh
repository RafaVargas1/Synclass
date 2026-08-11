#!/usr/bin/env bash
# Formata em stdin logs JSON compactos do Serilog (CompactJsonFormatter) para
# uma linha legível no terminal, colorida por nível e com o Environment em
# destaque — para diferenciar logs de dev e produção quando misturados no
# mesmo terminal (ver scripts/watch.sh).
#
# Uso:
#   dotnet watch run --project src/Synclass.Api | scripts/pretty-log.sh
#   docker compose logs -f --no-log-prefix api | scripts/pretty-log.sh

jq --unbuffered -R -r '
  (try fromjson catch null) as $e
  | if ($e == null or ($e | type) != "object") then . else
      ($e["@l"] // "Information") as $lvl
      | ($e.Environment // "-") as $env
      | ($e.TrackId // "-") as $trackId
      | ($e["@t"] // "") as $ts
      | (
          ($e["@mt"] // $e["@m"] // "") as $template
          | reduce ($e | to_entries[] | select(.key | startswith("@") | not)) as $p
              ($template; gsub("\\{" + ($p.key | gsub("[^a-zA-Z0-9_]"; "")) + "(:[^}]*)?\\}"; ($p.value | tostring)))
        ) as $msg
      | ($e["@x"] // "") as $ex
      | (if $lvl == "Fatal" or $lvl == "Error" then "[31m"
         elif $lvl == "Warning" then "[33m"
         elif $lvl == "Debug" or $lvl == "Verbose" then "[90m"
         else "[36m" end) as $color
      | "\($color)[\($env)][0m \($ts) \($color)\($lvl)[0m trackId=\($trackId) \($msg)"
        + (if $ex == "" then "" else "\n\($ex)" end)
    end
'
