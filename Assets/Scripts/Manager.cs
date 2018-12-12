﻿using System.Collections.Generic;
using UnityEngine;

public class Manager : MonoBehaviour
{
    public GameObject playerPrefab;
    private List<GameObject> playerList;
    private List<Genome> genomes;
    private List<Network> nets;
    private Dictionary<Genome, Network> networkMap;
    private Dictionary<Genome, Species> speciesMap;
    private List<Species> speciesList;
    private bool training;
    private int generation;
    public int population;
    public int inputNodes;
    public int outputNodes;
    public const float C1 = 1f;
    public const float C2 = 1f;
    public const float C3 = 0.3f;
    public const float compatiblityThreshold = 3f;
    public const float survivalChance = 0.1f;
    public const float weightMutationChance = 0.8f;
    public const float randomWeightChance = 0.1f;
    public const float addNodeChance = 0.03f;
    public const float addConnectionChance = 0.05f;


    void Start ()
    {
        training = false;
        generation = 1;
        genomes = new List<Genome>();
        speciesList = new List<Species>();
        playerList = new List<GameObject>();
        System.Random r = new System.Random();
        for(int i = 0; i<population; i++)
        {
            Genome genome = new Genome(inputNodes, outputNodes, r);
            genomes.Add(genome);
        }
	}
	
	
	void Update ()
    {
        if(!training)
        {
            AssignSpecies();
            MakePlayers();
            StartTraining();
        }

        if(TrainingComplete())
        {
            DestroyPlayers();
            SortNets();
            NextGen();
            training = false;
        }

    }

    private void AssignSpecies()
    {
        speciesMap = new Dictionary<Genome, Species>();
        foreach (Genome gen in genomes)
        {
            bool found = false;
            foreach (Species species in speciesList)
            {
                float distance = GenomeUtils.CompatiblityDistance(gen, species.GetMascot(), C1, C2, C3);
                if (distance < compatiblityThreshold)
                {
                    species.AddMember(gen);
                    speciesMap.Add(gen, species);
                    found = true;
                    break;
                }
            }

            if(!found)
            {
                Species species = new Species(gen);
                speciesList.Add(species);
                speciesMap.Add(gen, species);
            }
        }

        System.Random r = new System.Random();

        for(int i = speciesList.Count-1; i>=0; i--)
        {
            if(speciesList[i].GetCount()==0)
            {
                speciesList.RemoveAt(i);
            }
            else
            {
                speciesList[i].RandomizeMascot(r);
            }
        }

        Debug.Log("Gen: " + generation + ", Population: " + population + ", Species: " + speciesList.Count);
    }

    private void MakePlayers()
    {
        nets = new List<Network>();
        networkMap = new Dictionary<Genome, Network>();

        foreach (Genome genome in genomes)
        {
            Network net = new Network(genome);
            nets.Add(net);
            networkMap.Add(genome, net);
        }

        foreach (Network net in nets)
        {
            GameObject player = Instantiate(playerPrefab, playerPrefab.transform.position, playerPrefab.transform.rotation);
            playerList.Add(player);
            player.GetComponent<Movement>().SetNetwork(net);
        }
    }

    private void StartTraining()
    {
        training = true;
        foreach (GameObject player in playerList)
        {
            player.GetComponent<Movement>().Init();
        }
    }

    private bool TrainingComplete()
    {
        bool flag = true;
        foreach (GameObject player in playerList)
        {
            if(player.activeInHierarchy)
            {
                flag = false;
                break;
            }
        }
        return flag;
    }

    private void DestroyPlayers()
    {
        foreach(GameObject player in playerList)
        {
            Destroy(player);
        }
        playerList.Clear();
    }

    private void SortNets()
    {
        foreach (Network net in nets)
        {
            net.SetFitness(net.GetFitness()/speciesMap[net.GetGenome()].GetCount());
            speciesMap[net.GetGenome()].AddFitness(net.GetFitness());
        }

        nets.Sort();

        Debug.Log("Highest Fitness: " + nets[0].GetFitness() + ", Lowest Fitness: " + nets[population-1].GetFitness());
    }

    private void NextGen()
    {
        generation++;
        float totalFitness = 0;
        float leftPopulation = population * (1 - survivalChance);
        List<Genome> nextGenomes = new List<Genome>();

        foreach (Species species in speciesList)
        {
            totalFitness += species.GetFitness();
        }

        for (int i=0; i<(int)(population*survivalChance); i++)
        {
            nextGenomes.Add(nets[i].GetGenome());
        }

        System.Random r = new System.Random();

        foreach (Species species in speciesList)
        {
            for (int i=0; i< (int)species.GetFitness() / totalFitness * leftPopulation; i++)
            {
                Genome parent1 = species.GetRandomGenome(r);
                Genome parent2 = species.GetRandomGenome(r);
                Genome child = new Genome();

                if(networkMap[parent1].GetFitness()> networkMap[parent2].GetFitness())
                {
                    child = GenomeUtils.Crossover(parent1, parent2, r);
                }
                else
                {
                    child = GenomeUtils.Crossover(parent2, parent1, r);
                }
                nextGenomes.Add(child);
            }
        }

        foreach (Genome genome in nextGenomes)
        {
            double roll = r.NextDouble();

            if (roll < weightMutationChance)
            {
                genome.Mutate(randomWeightChance, r);
            }
            else if (roll < weightMutationChance + addNodeChance)
            {
                genome.AddNodeMutation(r);
            }
            else if (roll < weightMutationChance + addNodeChance + addConnectionChance)
            {
                genome.AddConnectionMutation(r);
            }
        }

        foreach (Species species in speciesList)
        {
            species.Reset();
        }
        population = nextGenomes.Count;
        genomes = nextGenomes;
    }

    public class Species
    {
        private Genome mascot;
        private List<Genome> members;
        private float fitness;

        public Species(Genome firstMember)
        {
            members = new List<Genome>
            {
                firstMember
            };
            mascot = firstMember;
            fitness = 0;
        }

        public Genome GetRandomGenome(System.Random r)
        {
            return members[r.Next(members.Count)];
        }

        public void RandomizeMascot(System.Random r)
        {
            mascot = members[r.Next(members.Count)];
        }

        public void AddMember(Genome genome)
        {
            members.Add(genome);
        }

        public void AddFitness(float fit)
        {
            fitness += fit;
        }

        public float GetFitness()
        {
            return fitness;
        }

        public int GetCount()
        {
            return members.Count;
        }

        public Genome GetMascot()
        {
            return mascot;
        }

        public void Reset()
        {
            members.Clear();
            fitness = 0;
        }

    }
}
