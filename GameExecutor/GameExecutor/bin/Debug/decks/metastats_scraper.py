import urllib.request
from bs4 import BeautifulSoup
import os
from shutil import copyfile

def get_decklist(url):
    source = urllib.request.urlopen(url)
    soup = BeautifulSoup(source, "lxml")
    deck = ""
    for cards in soup.find_all(class_="card-list-item"):
        cardname = cards.find('a').get_text().strip()
        quantity = cards.find(class_='card-quantity').get_text()
        deck = deck + quantity + "_" + cardname + "\n"
    return deck

def write_decklist(name, decklist):   
    with open(filename, 'a', encoding = 'utf-8') as f:
        f.write(name)
        f.write(decklist)

def create_deck_file(url):
    source = urllib.request.urlopen(url)
    soup = BeautifulSoup(source, "lxml")
    global filename
    filename = "meta_" + soup.head.find_all('meta')[3]["content"][-19:-9] + ".txt"

url = "http://metastats.net"
page = urllib.request.urlopen(url)
create_deck_file(url)
soup = BeautifulSoup(page, "lxml")
bound = 9
for topdecks in soup.find_all(id="archetype"):
    name = topdecks.get_text().replace(" ", "")
    link = url + topdecks.a['href']
    decklist = get_decklist(link)
    write_decklist(name, decklist)
    bound = bound - 1
    if (bound < 1):
        with open(filename, 'a', encoding = 'utf-8') as f: 
            f.write(name)
            break
copyfile(filename, os.path.realpath(os.path.join(os.getcwd(), "decks.txt")))
