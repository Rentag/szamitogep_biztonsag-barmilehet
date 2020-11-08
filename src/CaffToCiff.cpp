// CaffToCiff.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <iostream>
#include <fstream>
#include <string>
#include <algorithm>

using namespace std;

#pragma pack(push)
#pragma pack(1) //pragma pack to match an EXACT data layout
struct CaffHeader {
    char magic[4]; // 4 bytes
    uint64_t h_size; // 8 bytes
    uint64_t count; // 8 bytes
};
#pragma pack(pop)

#pragma pack(push)
#pragma pack(1)
struct CaffBlock {
    uint8_t blockType; // 1 byte
    uint64_t blockSize; // 8 bytes
};
#pragma pack(pop)

void generateCiff(const char* caffSrc, const char* ciffDst) {

    ifstream is(caffSrc, ios::binary | ios::in);
    if (!is.is_open()) { cerr << "File not found" << endl; exit(1); }
    ofstream os(ciffDst, ios::binary | ios::out);
    if (!os.is_open()) { is.close(); cerr << "File could not be created" << endl; exit(1); }

    CaffBlock cf; // 9 byte
    CaffHeader ch; // 20 byte
    bool readBlocks = true;

    if (!is.read((char*)&cf, sizeof(cf))) { cerr << "Failed to read metadata" << endl; readBlocks = false; } // read block metadata // in1.ciff 0x1 - header

    if (!is.read((char*)&ch, sizeof(ch))) { cerr << "Failed to read header block" << endl; readBlocks = false; } // read header block

    if (strncmp(ch.magic, "CAFF", 4)) { cerr << "Wrong file format" << endl; readBlocks = false; } // magic is not null-terminated
    
    while (readBlocks) {
        if (!is.read((char*)&cf, sizeof(cf))) { cerr << "Failed to read metadata" << endl; break; } // read block metadata // in1.coff 0x2 credentials or 0x3 animation
        
        switch (cf.blockType)
        {
        case 0x2:
            is.seekg(cf.blockSize, ios::cur);
            break;
        case 0x3:
            copy_n(istreambuf_iterator<char>(is.seekg(8, ios::cur)), cf.blockSize - 8, ostreambuf_iterator<char>(os)); // copy the content of ciff into .ciff file
            readBlocks = false;
            break;
        default:
            cerr << "invalid block type" << endl;
            readBlocks = false;
            break;
        }
    }

    is.close();
    os.close();
}

int main(int argc, char* argv[])
{
    const char* caffSrc = argv[1];
    const char* ciffDst = argv[2];

    generateCiff(caffSrc, ciffDst);
}
