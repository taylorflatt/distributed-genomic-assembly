<em>Taylor Flatt, Marty Grawe, Ryan Sample, Michelle Zhu, PhD, Matt Geisler, PhD
Department of Computer Science and the Department of Plant Biology
A Parallel Genome Assembly and Analysis System using Big Dog Cluster at SIUC</em>

The goal of this project is to create a High Performance Computing (HPC) infrastructure for large-scale genome data analysis. The parallelized computational pipeline includes multiple genome assemblers, user web interface, SQL database and statistical and graphical output to support fast and convenient genome analysis for biologists. Various computing and memory intensive tasks can be integrated and executed in an automated way to reduce human interference. There are three major objectives in this project. 

The first is to construct the parallelized process on a distributed system which significantly speeds up the assembly and analysis of the genome. Initial tests on moderately sized data showed a decrease in time from 3 days on a typical computer to 3 hours on the high memory nodes of the Big Dog cluster (i.e., SIUC newly purchased HPC cluster). 

The second objective is to provide the most accurate results possible. Running the data in parallel through many different assemblers will likely produce very different outputs. Running a BLAST efficiency analysis on our several outputs, we can construct a more complete picture than any one algorithm can produce. Then we will assemble the contigs using a CAP3 program to produce the final output. 

Our final main objective will be to create a web interface for easy integration with the parallelized system. This would allow researchers the ability to seamlessly access the cluster to view, modify, and create jobs for assembly. This approach allows researchers to easily select which assemblers and parameters they wish to use for the assembly of their data. The added benefit is that this approach requires little to no requisite knowledge of UNIX systems simplifying the process altogether.

Ultimately, the project’s focus on increased efficiency, accuracy, and accessibility for genomic research will hopefully make it easier for researchers to conduct their research. 
