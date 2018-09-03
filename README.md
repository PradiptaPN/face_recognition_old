# PengenalanExpresiWajah

Merupakan Proyek TA171801038 dengan anggota:
Jonatan, 
Faisal Rasbihan, 
Pradipta Prayoga Nugraha.

# Deskripsi Project

Project PengenalanEkspresiWajah merupakan salah satu proyek Tugas Akhir Mahasiswa Teknik Elektro tahun 2017/2018. Di dalam proyek ini terdapat program untuk mendeteksi wajah, mengenal wajah, mengenal ekspresi wajah, melatih neural network yang akan dipakai untuk pengenalan wajah dan ekspresi wajah, dan mengambil wajah untuk dijadikan dataset. 
Di dalam satu solution ini, terdapat tiga buah project, yaitu IdentifikasiEkspresiWajah, TrainFRNetwork, dan TrainFERNetwork.

- IdentifikasiEkspresiWajah merupakan program utama untuk mengenali wajah dan mengenali ekspresi wajah, kemudian memasukkan hasil pembacaan tersebut
 ke dalam grafik dan dapat di download dengan menekan tombol di sebelah grafik tersebut.
- TrainFRNetwork merupakan program untuk melatih network yang digunakan untuk mengenali wajah.
- TrainFERNetwork merupakan program untuk melatih network yang digunakan untuk mengenali ekspresi wajah.

- Untuk dataset ekspresi wajah, digunakan CK+ Helen dataset (sementara).
- Untuk dataset pengenalan wajah, digunakan dataset sendiri dengan jumlah wajah 3 x 5 wajah. (sementara)


# Installing Dependencies:

- Emgu CV - Install melalui NuGet packages
- Newtonsoft JSON - Install melalui NuGet packages 
- LiveCharts WPF - Install melalui NuGet packages
- ConvNetSharp (source: https://github.com/cbovar/ConvNetSharp)
	- setelah menginstall ketiga dependencies diatas, akan ada folder packages yang berisi dependencies. Unrar convnetsharp.rar dan pindahkan ke dalam folder packages tersebut. Kemudian cek solution apakah sudah terdeteksi oleh visual studio atau belum.
	- apabila belum terdeteksi, add reference dan masukkan ConvNetSharp.Core.dll dan ConvNetSharp.Volume.dll yang dapat ditemukan di directory \IdentifikasiEkspresiWajah\packages\ConvNetSharp\src\ConvNetSharp.Core\bin\Debug.


